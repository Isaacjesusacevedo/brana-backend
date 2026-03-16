using EsotericStore.API.Data;
using EsotericStore.API.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace EsotericStore.API.Services;

public class CategoryService : ICategoryService
{
    private readonly AppDbContext _context;
    private readonly IProductService _productService;

    public CategoryService(AppDbContext context, IProductService productService)
    {
        _context = context;
        _productService = productService;
    }

    // ── Todas las categorías con sus productos ────────────────────────────────
    public async Task<List<CategoryDto>> GetAllAsync()
    {
        var categories = await _context.Categories
            .Include(c => c.Productos)
                .ThenInclude(p => p.Imagenes.OrderBy(i => i.Orden))
            .Include(c => c.Productos)
                .ThenInclude(p => p.Colores)
            .OrderBy(c => c.Id)
            .ToListAsync();

        return categories.Select(c => new CategoryDto
        {
            Id          = c.Id,
            Slug        = c.Slug,
            Nombre      = c.Nombre,
            Descripcion = c.Descripcion,
            Icono       = c.Icono,
            Ruta        = c.Ruta,
            Productos   = c.Productos.Select(ProductService.MapToDto).ToList()
        }).ToList();
    }

    // ── Una categoría por slug con sus productos paginados ────────────────────
    public async Task<CategoryDto?> GetBySlugAsync(string slug, ProductQueryParams query)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Slug == slug.ToLower());

        if (category is null) return null;

        // Reusar el filtro y paginación de IProductService
        query.Categoria = slug;
        var paginado = await _productService.GetAllAsync(query);

        return new CategoryDto
        {
            Id          = category.Id,
            Slug        = category.Slug,
            Nombre      = category.Nombre,
            Descripcion = category.Descripcion,
            Icono       = category.Icono,
            Ruta        = category.Ruta,
            Productos   = paginado.Items
        };
    }
}

// ─── Interface ────────────────────────────────────────────────────────────────
public interface ICategoryService
{
    Task<List<CategoryDto>> GetAllAsync();

    /// <summary>
    /// Retorna la categoría con sus productos paginados.
    /// Retorna null si el slug no existe.
    /// </summary>
    Task<CategoryDto?> GetBySlugAsync(string slug, ProductQueryParams query);
}
