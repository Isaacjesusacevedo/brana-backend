using EsotericStore.API.Models.DTOs;
using EsotericStore.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EsotericStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    /// <summary>
    /// Todas las categorías con sus productos.
    /// Reemplaza el objeto reactivo 'categorias' en HomeView.vue
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryDto>>), 200)]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();
        return Ok(ApiResponse<List<CategoryDto>>.Ok(categories));
    }

    /// <summary>
    /// Una categoría por slug con sus productos paginados.
    /// GET /api/categories/remeras
    /// GET /api/categories/buzos?page=1&amp;pageSize=8
    /// </summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetBySlug(string slug, [FromQuery] ProductQueryParams query)
    {
        var category = await _categoryService.GetBySlugAsync(slug, query);
        if (category is null)
            return NotFound(ApiResponse<CategoryDto>.Fail($"Categoría '{slug}' no encontrada"));

        return Ok(ApiResponse<CategoryDto>.Ok(category));
    }
}
