// ─── OrdersController.cs ─────────────────────────────────────────────────────
using EsotericStore.API.Data;
using EsotericStore.API.Models.DTOs;
using EsotericStore.API.Models.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsotericStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public OrdersController(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Crear un pedido personalizado.
    /// Este es el endpoint principal para la tienda sin pago integrado.
    /// El pedido se guarda y se puede ver en el panel admin.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 201)]
    public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        // Verificar que el producto existe
        var productoExiste = await _context.Products.AnyAsync(p => p.Id == dto.ProductoId);
        if (!productoExiste)
            return BadRequest(ApiResponse<OrderDto>.Fail("Producto no encontrado"));

        var order = new Order
        {
            Nombre           = dto.Nombre,
            Email            = dto.Email,
            Telefono         = dto.Telefono,
            ProductoId       = dto.ProductoId,
            Talla            = dto.Talla,        // ✅ CORREGIDO: era dto.Talle
            Color            = dto.Color,
            Cantidad         = dto.Cantidad,
            NotasAdicionales = dto.NotasAdicionales,
            Estado           = "pendiente"
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var orderDto = await GetOrderDto(order.Id);
        return CreatedAtAction(nameof(GetById), new { id = order.Id },
            ApiResponse<OrderDto>.Ok(orderDto!, "Pedido recibido. Te contactaremos pronto."));
    }

    /// <summary>Ver el detalle de un pedido por su ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await GetOrderDto(id);
        if (order is null)
            return NotFound(ApiResponse<OrderDto>.Fail("Pedido no encontrado"));

        return Ok(ApiResponse<OrderDto>.Ok(order));
    }

    /// <summary>Listar todos los pedidos (para panel admin)</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OrderDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] string? estado = null)
    {
        var q = _context.Orders
            .Include(o => o.Producto)
                .ThenInclude(p => p.Imagenes)
            .Include(o => o.Producto)
                .ThenInclude(p => p.Categoria)
            .Include(o => o.Producto)
                .ThenInclude(p => p.Colores)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(estado))
            q = q.Where(o => o.Estado == estado);

        var orders = await q.OrderByDescending(o => o.FechaPedido).ToListAsync();
        var dtos = orders.Select(MapToDto).ToList();

        return Ok(ApiResponse<List<OrderDto>>.Ok(dtos));
    }

    /// <summary>Actualizar el estado de un pedido</summary>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 200)]
    public async Task<IActionResult> UpdateEstado(Guid id, [FromBody] string nuevoEstado)
    {
        var estadosValidos = new[] { "pendiente", "confirmado", "enviado", "entregado", "cancelado" };
        if (!estadosValidos.Contains(nuevoEstado))
            return BadRequest(ApiResponse<OrderDto>.Fail($"Estado inválido. Válidos: {string.Join(", ", estadosValidos)}"));

        var order = await _context.Orders.FindAsync(id);
        if (order is null)
            return NotFound(ApiResponse<OrderDto>.Fail("Pedido no encontrado"));

        order.Estado = nuevoEstado;
        await _context.SaveChangesAsync();

        var dto = await GetOrderDto(id);
        return Ok(ApiResponse<OrderDto>.Ok(dto!));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────
    private async Task<OrderDto?> GetOrderDto(Guid id)
    {
        var order = await _context.Orders
            .Include(o => o.Producto)
                .ThenInclude(p => p.Imagenes.OrderBy(i => i.Orden))
            .Include(o => o.Producto)
                .ThenInclude(p => p.Categoria)
            .Include(o => o.Producto)
                .ThenInclude(p => p.Colores)
            .FirstOrDefaultAsync(o => o.Id == id);

        return order is null ? null : MapToDto(order);
    }

    private static OrderDto MapToDto(Order o)
    {
        var imagenes = o.Producto?.Imagenes.OrderBy(i => i.Orden).Select(i => i.Url).ToList() ?? new();

        return new OrderDto
        {
            Id               = o.Id,
            Nombre           = o.Nombre,
            Email            = o.Email,
            Telefono         = o.Telefono,
            Estado           = o.Estado,
            FechaPedido      = o.FechaPedido,
            Talla            = o.Talla,          // ✅ CORREGIDO: era o.Talle
            Color            = o.Color,
            Cantidad         = o.Cantidad,
            NotasAdicionales = o.NotasAdicionales,
            Producto = o.Producto is null ? null : new ProductDto
            {
                Id       = o.Producto.Id,
                Nombre   = o.Producto.Nombre,
                Precio   = o.Producto.Precio,
                Imagen   = imagenes.FirstOrDefault() ?? string.Empty,
                Imagenes = imagenes,
                Categoria = o.Producto.Categoria?.Nombre ?? string.Empty,
                Colores  = o.Producto.Colores.Select(c => c.Hex).ToList()
            }
        };
    }
}