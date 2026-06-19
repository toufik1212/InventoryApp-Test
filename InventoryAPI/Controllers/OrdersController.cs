using InventoryService.Interfaces;
using InventoryShared.Dtos;
using InventoryShared.Dtos.Orders;
using Microsoft.AspNetCore.Mvc;

namespace InventoryAPI.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpPost]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequestDto request,
        [FromHeader(Name = "Idempotency-Key")] string idempotencyKey)
    {
        var result = await _orderService.CreateOrderAsync(request, idempotencyKey);
        return CreatedAtAction(nameof(GetOrder), new { id = result.Id }, result);
    }

    [HttpGet("{id:long}")]
    public async Task<IActionResult> GetOrder(long id)
    {
        var result = await _orderService.GetOrderByIdAsync(id);

        if (result == null)
            return NotFound(new { message = "Order tidak ditemukan" });

        return Ok(result);
    }

    [HttpGet]
    public async Task<IActionResult> GetOrders([FromQuery] OrderListRequestDto request)
    {
        var result = await _orderService.GetOrdersAsync(request);
        return Ok(result);
    }

    [HttpPut("{id:long}/status")]
    public async Task<IActionResult> UpdateStatus(
    long id,
    [FromBody] UpdateOrderStatusRequestDto request)
    {
        var result = await _orderService.UpdateStatusAsync(id, request);
        return Ok(result);
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> CancelOrder(long id)
    {
        var result = await _orderService.CancelOrderAsync(id);
        return Ok(result);
    }

}