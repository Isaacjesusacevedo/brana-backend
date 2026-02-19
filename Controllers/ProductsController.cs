using EsotericStore.API.Models.DTOs;
using EsotericStore.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EsotericStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    /// <summary>Lista de productos con filtros opcionales y paginación</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<ProductDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] ProductQueryParams query)
    {
        var result = await _productService.GetAllAsync(query);
        return Ok(ApiResponse<PagedResponse<ProductDto>>.Ok(result));
    }

    /// <summary>Piezas destacadas para la sección Hero de la home</summary>
    [HttpGet("featured")]
    [ProducesResponseType(typeof(ApiResponse<List<ProductDto>>), 200)]
    public async Task<IActionResult> GetFeatured([FromQuery] int limit = 6)
    {
        var result = await _productService.GetFeaturedAsync(limit);
        return Ok(ApiResponse<List<ProductDto>>.Ok(result));
    }

    /// <summary>Detalle completo de un producto</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        var product = await _productService.GetByIdAsync(id);
        if (product is null)
            return NotFound(ApiResponse<ProductDto>.Fail($"Producto '{id}' no encontrado"));

        return Ok(ApiResponse<ProductDto>.Ok(product));
    }

    /// <summary>Crear un producto nuevo</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] ProductCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var product = await _productService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = product.Id },
            ApiResponse<ProductDto>.Ok(product, "Producto creado exitosamente"));
    }

    /// <summary>Actualizar un producto existente</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<ProductDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] ProductUpdateDto dto)
    {
        var product = await _productService.UpdateAsync(id, dto);
        if (product is null)
            return NotFound(ApiResponse<ProductDto>.Fail($"Producto '{id}' no encontrado"));

        return Ok(ApiResponse<ProductDto>.Ok(product, "Producto actualizado"));
    }

    /// <summary>Eliminar un producto</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(string id)
    {
        var deleted = await _productService.DeleteAsync(id);
        if (!deleted)
            return NotFound(ApiResponse<bool>.Fail($"Producto '{id}' no encontrado"));

        return NoContent();
    }
}