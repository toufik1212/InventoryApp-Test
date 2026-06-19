using InventoryShared;
using InventoryShared.Dtos;
using InventoryShared.Dtos.Orders;

namespace InventoryService.Interfaces;

public interface IOrderService
{
    Task<GetOrderResponseDto> CreateOrderAsync(
        CreateOrderRequestDto request,
        string idempotencyKey
    );

    Task<GetOrderResponseDto?> GetOrderByIdAsync(long id);

    Task<PagedResultDto<GetOrderResponseDto>> GetOrdersAsync(OrderListRequestDto request);
    Task<GetOrderResponseDto> UpdateStatusAsync(long orderId,UpdateOrderStatusRequestDto request);
    Task<GetOrderResponseDto> CancelOrderAsync(long orderId);
}