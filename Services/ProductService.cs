using EsotericStore.API.Data;
using EsotericStore.API.Models.DTOs;
using EsotericStore.API.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace EsotericStore.API.Services;

public class ProductService : IProductService
{
    private readonly AppDbContext _context;

    public ProductService(AppDbContext context)
    {
        _context = context;
    }

    // ── Query base reutilizable ───────────────────────────────────────────────
    private IQueryable<Product> BaseQuery() =>
        _context.Products
            .Include(p => p.Categoria)
            .Include(p => p.Imagenes.OrderBy(i => i.Orden))
            .Include(p => p.Colores);

    // ── GetAll con filtros y paginación ──────────────────────────────────────
    public async Task<PagedResponse<ProductDto>> GetAllAsync(ProductQueryParams query)
    {
        var q = BaseQuery().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Categoria))
            q = q.Where(p => p.Categoria.Slug == query.Categoria.ToLower());

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p =>
                (p.Nombre != null && p.Nombre.Contains(query.Search)) ||
                p.Descripcion.Contains(query.Search));

        if (query.MinPrecio.HasValue)
            q = q.Where(p => p.Precio >= query.MinPrecio.Value);

        if (query.MaxPrecio.HasValue)
            q = q.Where(p => p.Precio <= query.MaxPrecio.Value);

        if (query.SoloNuevos == true)
            q = q.Where(p => p.Nuevo);

        q = query.OrderBy switch
        {
            "precio" => query.Desc ? q.OrderByDescending(p => p.Precio) : q.OrderBy(p => p.Precio),
            "nuevo"  => q.OrderByDescending(p => p.Nuevo),
            _        => query.Desc ? q.OrderByDescending(p => p.Nombre) : q.OrderBy(p => p.Nombre),
        };

        var total = await q.CountAsync();
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new PagedResponse<ProductDto>
        {
            Items = items.Select(MapToDto).ToList(),
            Total = total,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    // ── Featured — para HomePiezasDestacadas ─────────────────────────────────
    public async Task<List<ProductDto>> GetFeaturedAsync(int limit = 6)
    {
        var products = await BaseQuery()
            .Where(p => p.Badge != null || p.Nuevo || p.Size == "featured" || p.Size == "wide" || p.Size == "tall")
            .OrderByDescending(p => p.Nuevo)
            .Take(limit)
            .ToListAsync();

        return products.Select(MapToDto).ToList();
    }

    // ── GetById ───────────────────────────────────────────────────────────────
    public async Task<ProductDto?> GetByIdAsync(string id)
    {
        var product = await BaseQuery().FirstOrDefaultAsync(p => p.Id == id);
        return product is null ? null : MapToDto(product);
    }

    // ── Por categoría — para CategorySection en HomeView ─────────────────────
    public async Task<List<ProductDto>> GetByCategoriaAsync(string slug)
    {
        var products = await BaseQuery()
            .Where(p => p.Categoria.Slug == slug.ToLower())
            .ToListAsync();

        return products.Select(MapToDto).ToList();
    }

    // ── Create ────────────────────────────────────────────────────────────────
    public async Task<ProductDto> CreateAsync(ProductCreateDto dto)
    {
        var product = new Product
        {
            Nombre          = dto.Nombre,
            Titulo          = dto.Titulo,
            Descripcion     = dto.Descripcion,
            Precio          = dto.Precio,
            PrecioAnterior  = dto.PrecioAnterior,
            CategoriaId     = dto.CategoriaId,
            Badge           = dto.Badge,
            Size            = dto.Size,
            Nuevo           = dto.Nuevo,
            Stock           = dto.Stock,
            Ruta            = dto.Ruta,
            TallasJson          = dto.Tallas is not null ? JsonSerializer.Serialize(dto.Tallas) : null,
            CaracteristicasJson = dto.Caracteristicas is not null ? JsonSerializer.Serialize(dto.Caracteristicas) : null,
            Imagenes = dto.ImagenUrls.Select((url, i) => new ProductImage
            {
                Url = url, EsPrincipal = i == 0, Orden = i
            }).ToList(),
            Colores = dto.Colores.Select(hex => new ProductColor { Hex = hex }).ToList()
        };

        _context.Products.Add(product);
        await _context.SaveChangesAsync();
        return (await GetByIdAsync(product.Id))!;
    }

    // ── Update ────────────────────────────────────────────────────────────────
    public async Task<ProductDto?> UpdateAsync(string id, ProductUpdateDto dto)
    {
        var product = await _context.Products
            .Include(p => p.Imagenes)
            .Include(p => p.Colores)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product is null) return null;

        product.Nombre          = dto.Nombre;
        product.Titulo          = dto.Titulo;
        product.Descripcion     = dto.Descripcion;
        product.Precio          = dto.Precio;
        product.PrecioAnterior  = dto.PrecioAnterior;
        product.CategoriaId     = dto.CategoriaId;
        product.Badge           = dto.Badge;
        product.Size            = dto.Size;
        product.Nuevo           = dto.Nuevo;
        product.Stock           = dto.Stock;
        product.Ruta            = dto.Ruta;
        product.TallasJson          = dto.Tallas is not null ? JsonSerializer.Serialize(dto.Tallas) : null;
        product.CaracteristicasJson = dto.Caracteristicas is not null ? JsonSerializer.Serialize(dto.Caracteristicas) : null;

        _context.ProductImages.RemoveRange(product.Imagenes);
        product.Imagenes = dto.ImagenUrls.Select((url, i) => new ProductImage
        {
            Url = url, EsPrincipal = i == 0, Orden = i, ProductoId = product.Id
        }).ToList();

        _context.ProductColors.RemoveRange(product.Colores);
        product.Colores = dto.Colores.Select(hex => new ProductColor
        {
            Hex = hex, ProductoId = product.Id
        }).ToList();

        await _context.SaveChangesAsync();
        return (await GetByIdAsync(product.Id))!;
    }

    // ── Delete ────────────────────────────────────────────────────────────────
    public async Task<bool> DeleteAsync(string id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product is null) return false;

        _context.Products.Remove(product);
        await _context.SaveChangesAsync();
        return true;
    }

    // ── Mapper: Entity → DTO (refleja exactamente la interfaz Product de TS) ──
    public static ProductDto MapToDto(Product p)
    {
        var todasImagenes = p.Imagenes.OrderBy(i => i.Orden).Select(i => i.Url).ToList();

        return new ProductDto
        {
            Id             = p.Id,
            Nombre         = p.Nombre,
            Titulo         = p.Titulo,
            Imagen         = todasImagenes.FirstOrDefault() ?? string.Empty,
            Imagenes       = todasImagenes.Count > 0 ? todasImagenes : null,
            Precio         = p.Precio,
            PrecioAnterior = p.PrecioAnterior,
            Categoria      = p.Categoria?.Nombre,
            Nuevo          = p.Nuevo,
            Colores        = p.Colores.Count > 0 ? p.Colores.Select(c => c.Hex).ToList() : null,
            Descripcion    = string.IsNullOrEmpty(p.Descripcion) ? null : p.Descripcion,
            Caracteristicas = p.CaracteristicasJson is not null
                ? JsonSerializer.Deserialize<List<string>>(p.CaracteristicasJson)
                : null,
            Stock  = p.Stock,
            Tallas = p.TallasJson is not null
                ? JsonSerializer.Deserialize<List<string>>(p.TallasJson)
                : null,
            Ruta  = p.Ruta,
            Badge = p.Badge,
            Size  = p.Size,
        };
    }
}

// ─── Interface ────────────────────────────────────────────────────────────────
public interface IProductService
{
    Task<PagedResponse<ProductDto>> GetAllAsync(ProductQueryParams query);
    Task<List<ProductDto>> GetFeaturedAsync(int limit = 6);
    Task<ProductDto?> GetByIdAsync(string id);
    Task<List<ProductDto>> GetByCategoriaAsync(string slug);
    Task<ProductDto> CreateAsync(ProductCreateDto dto);
    Task<ProductDto?> UpdateAsync(string id, ProductUpdateDto dto);
    Task<bool> DeleteAsync(string id);
}