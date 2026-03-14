using EsotericStore.API.Models.DTOs;
using EsotericStore.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace EsotericStore.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    /// <summary>
    /// Crear un pedido como invitado.
    /// El pedido queda en estado "pendiente" hasta que el admin lo confirme.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] OrderCreateDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var order = await _orderService.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = order.Id },
            ApiResponse<OrderDto>.Ok(order, "Pedido recibido. Te contactaremos pronto."));
    }

    /// <summary>Ver el detalle de un pedido por su ID</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var order = await _orderService.GetByIdAsync(id);
        if (order is null)
            return NotFound(ApiResponse<OrderDto>.Fail("Pedido no encontrado"));

        return Ok(ApiResponse<OrderDto>.Ok(order));
    }

    /// <summary>Listar todos los pedidos (para panel admin). Filtrar con ?estado=pendiente</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<OrderDto>>), 200)]
    public async Task<IActionResult> GetAll([FromQuery] string? estado = null)
    {
        var orders = await _orderService.GetAllAsync(estado);
        return Ok(ApiResponse<List<OrderDto>>.Ok(orders));
    }

    /// <summary>Actualizar el estado de un pedido</summary>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(ApiResponse<OrderDto>), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateEstado(Guid id, [FromBody] string nuevoEstado)
    {
        var order = await _orderService.UpdateEstadoAsync(id, nuevoEstado);
        if (order is null)
            return NotFound(ApiResponse<OrderDto>.Fail("Pedido no encontrado"));

        return Ok(ApiResponse<OrderDto>.Ok(order));
    }
}
