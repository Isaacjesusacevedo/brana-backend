using EsotericStore.API.Data;
using EsotericStore.API.Models.DTOs;
using EsotericStore.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EsotericStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IProductService _productService;

    public CategoriesController(AppDbContext context, IProductService productService)
    {
        _context = context;
        _productService = productService;
    }

    /// <summary>
    /// Todas las categorías.
    /// Reemplaza el objeto reactivo 'categorias' en HomeView.vue
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _context.Categories
            .Include(c => c.Productos)
                .ThenInclude(p => p.Imagenes.OrderBy(i => i.Orden))
            .Include(c => c.Productos)
                .ThenInclude(p => p.Colores)
            .OrderBy(c => c.Id)
            .ToListAsync();

        var dtos = categories.Select(c => new CategoryDto
        {
            Id          = c.Id,
            Slug        = c.Slug,
            Nombre      = c.Nombre,
            Descripcion = c.Descripcion,
            Icono       = c.Icono,
            Ruta        = c.Ruta,
            Productos   = c.Productos.Select(ProductService.MapToDto).ToList()
        }).ToList();

        return Ok(ApiResponse<List<CategoryDto>>.Ok(dtos));
    }

    /// <summary>
    /// Una categoría por slug con sus productos paginados.
    /// GET /api/categories/remeras
    /// GET /api/categories/buzos?page=1&pageSize=8
    /// </summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBySlug(string slug, [FromQuery] ProductQueryParams query)
    {
        var category = await _context.Categories
            .FirstOrDefaultAsync(c => c.Slug == slug.ToLower());

        if (category is null)
            return NotFound(ApiResponse<CategoryDto>.Fail($"Categoría '{slug}' no encontrada"));

        // Reusar el filtro del ProductService
        query.Categoria = slug;
        var paginado = await _productService.GetAllAsync(query);

        var dto = new CategoryDto
        {
            Id          = category.Id,
            Slug        = category.Slug,
            Nombre      = category.Nombre,
            Descripcion = category.Descripcion,
            Icono       = category.Icono,
            Ruta        = category.Ruta,
            Productos   = paginado.Items
        };

        return Ok(ApiResponse<CategoryDto>.Ok(dto));
    }
}