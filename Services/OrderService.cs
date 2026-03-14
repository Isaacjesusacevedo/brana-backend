using EsotericStore.API.Data;
using EsotericStore.API.Models.DTOs;
using EsotericStore.API.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EsotericStore.API.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _context;

    private static readonly string[] EstadosValidos =
        ["pendiente", "confirmado", "enviado", "entregado", "cancelado"];

    public OrderService(AppDbContext context)
    {
        _context = context;
    }

    // ── Query base con todos los includes necesarios ──────────────────────────
    private IQueryable<Order> BaseQuery() =>
        _context.Orders
            .Include(o => o.Producto)
                .ThenInclude(p => p.Imagenes.OrderBy(i => i.Orden))
            .Include(o => o.Producto)
                .ThenInclude(p => p.Categoria)
            .Include(o => o.Producto)
                .ThenInclude(p => p.Colores);

    // ── Create ────────────────────────────────────────────────────────────────
    public async Task<OrderDto> CreateAsync(OrderCreateDto dto)
    {
        var productoExiste = await _context.Products.AnyAsync(p => p.Id == dto.ProductoId);
        if (!productoExiste)
            throw new ArgumentException("Producto no encontrado");

        var order = new Order
        {
            Nombre           = dto.Nombre,
            Email            = dto.Email,
            Telefono         = dto.Telefono,
            ProductoId       = dto.ProductoId,
            Talla            = dto.Talla,
            Color            = dto.Color,
            Cantidad         = dto.Cantidad,
            NotasAdicionales = dto.NotasAdicionales,
            Estado           = "pendiente"
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return (await GetByIdAsync(order.Id))!;
    }

    // ── GetById ───────────────────────────────────────────────────────────────
    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var order = await BaseQuery().FirstOrDefaultAsync(o => o.Id == id);
        return order is null ? null : MapToDto(order);
    }

    // ── GetAll con filtro de estado opcional ──────────────────────────────────
    public async Task<List<OrderDto>> GetAllAsync(string? estado = null)
    {
        var q = BaseQuery().AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            q = q.Where(o => o.Estado == estado);

        var orders = await q.OrderByDescending(o => o.FechaPedido).ToListAsync();
        return orders.Select(MapToDto).ToList();
    }

    // ── UpdateEstado ──────────────────────────────────────────────────────────
    public async Task<OrderDto?> UpdateEstadoAsync(Guid id, string nuevoEstado)
    {
        if (!EstadosValidos.Contains(nuevoEstado))
            throw new ArgumentException(
                $"Estado inválido. Válidos: {string.Join(", ", EstadosValidos)}");

        var order = await _context.Orders.FindAsync(id);
        if (order is null) return null;

        order.Estado = nuevoEstado;
        await _context.SaveChangesAsync();

        return await GetByIdAsync(id);
    }

    // ── Mapper: Entity → DTO ──────────────────────────────────────────────────
    private static OrderDto MapToDto(Order o)
    {
        var imagenes = o.Producto?.Imagenes.OrderBy(i => i.Orden).Select(i => i.Url).ToList() ?? [];

        return new OrderDto
        {
            Id               = o.Id,
            Nombre           = o.Nombre,
            Email            = o.Email,
            Telefono         = o.Telefono,
            Estado           = o.Estado,
            FechaPedido      = o.FechaPedido,
            Talla            = o.Talla,
            Color            = o.Color,
            Cantidad         = o.Cantidad,
            NotasAdicionales = o.NotasAdicionales,
            Producto = o.Producto is null ? null : new ProductDto
            {
                Id        = o.Producto.Id,
                Nombre    = o.Producto.Nombre,
                Precio    = o.Producto.Precio,
                Imagen    = imagenes.FirstOrDefault() ?? string.Empty,
                Imagenes  = imagenes,
                Categoria = o.Producto.Categoria?.Nombre ?? string.Empty,
                Colores   = o.Producto.Colores.Select(c => c.Hex).ToList()
            }
        };
    }
}

// ─── Interface ────────────────────────────────────────────────────────────────
public interface IOrderService
{
    /// <summary>Crea un pedido. Lanza ArgumentException si el producto no existe.</summary>
    Task<OrderDto> CreateAsync(OrderCreateDto dto);

    Task<OrderDto?> GetByIdAsync(Guid id);
    Task<List<OrderDto>> GetAllAsync(string? estado = null);

    /// <summary>
    /// Actualiza el estado de un pedido.
    /// Lanza ArgumentException si el estado no es válido.
    /// Retorna null si el pedido no existe.
    /// </summary>
    Task<OrderDto?> UpdateEstadoAsync(Guid id, string nuevoEstado);
}
