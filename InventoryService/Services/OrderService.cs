using InventoryCore.Entities;
using InventoryCore.Enums;
using InventoryData;
using InventoryData.Context;
using InventoryService.Exceptions;
using InventoryService.Interfaces;
using InventoryShared;
using InventoryShared.Dtos;
using InventoryShared.Dtos.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Security.Cryptography;

namespace InventoryService.Services;

public class OrderService : IOrderService
{
    private readonly AppDbContext _db;

    private readonly ILogger<OrderService> _logger;

    public OrderService(AppDbContext db, ILogger<OrderService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<GetOrderResponseDto> CreateOrderAsync(
        CreateOrderRequestDto request,
        string idempotencyKey)
    {
        var requestHash = CreateRequestHash(request);

        if (string.IsNullOrWhiteSpace(idempotencyKey))
            throw new AppException("Idempotency-Key is required", 400);

        if (request.Items == null || request.Items.Count == 0)
            throw new AppException("Order item tidak boleh kosong", 422);

        using var transaction = await _db.Database.BeginTransactionAsync();

        var existingKey = await _db.IdempotencyRequests
            .FirstOrDefaultAsync(x => x.Key == idempotencyKey);

        if (existingKey != null)
        {
            if (existingKey.RequestHash != requestHash)
                throw new AppException("Idempotency-Key sudah dipakai untuk request yang berbeda", 409);

            if (existingKey.OrderId.HasValue)
            {
                return await GetOrderResponseAsync(existingKey.OrderId.Value);
            }

            throw new AppException("Request sedang diproses", 409);
        }

        var idem = new IdempotencyRequest
        {
            Key = idempotencyKey,
            RequestHash = requestHash,
            Status = "Processing"
        };

        _db.IdempotencyRequests.Add(idem);
        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new AppException(
                "Request dengan Idempotency-Key yang sama sedang diproses",
                409
            );
        }

        var order = new Order
        {
            CustomerId = request.CustomerId,
            ShippAddress = request.ShippingAddress,
            Status = OrderStatus.Pending,
            OrderDate = DateTime.UtcNow
        };

        foreach (var item in request.Items)
        {
            var product = await _db.Products
                .FirstOrDefaultAsync(x => x.Id == item.ProductId);

            if (product == null)
                throw new AppException($"Product {item.ProductId} tidak ditemukan", 404);

            //atomix sql

            var affected = await _db.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE Products
                SET StockQty = StockQty - {item.Quantity}
                WHERE Id = {item.ProductId}
                AND StockQty >= {item.Quantity}
            ");

            if (affected == 0)
                throw new AppException($"Stock product {product.Product_Name} tidak cukup",409);

            order.Items.Add(new OrderItem
            {
                ProductId = product.Id,
                Quantity = item.Quantity,
                UnitPrice = product.Price
            });
        }

        _db.Orders.Add(order);
        await _db.SaveChangesAsync();

        idem.OrderId = order.Id;
        idem.Status = "Completed";

        _logger.LogInformation(
            "Create order request received. CustomerId: {CustomerId}, IdempotencyKey: {IdempotencyKey}",
            request.CustomerId,
            idempotencyKey
        );

        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        return await GetOrderResponseAsync(order.Id);
    }

    private async Task<GetOrderResponseDto> GetOrderResponseAsync(long orderId)
    {
        var order = await _db.Orders
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstAsync(x => x.Id == orderId);

        return new GetOrderResponseDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            ShippingAddress = order.ShippAddress,
            Status = order.Status.ToString(),
            OrderDate = order.OrderDate,
            RowVersion = Convert.ToBase64String(order.RowVersion),
            Items = order.Items.Select(x => new GetOrderItemDto
            {
                ProductId = x.ProductId,
                ProductName = x.Product.Product_Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice
            }).ToList()
        };
    }

    public async Task<GetOrderResponseDto?> GetOrderByIdAsync(long id)
    {
        var order = await _db.Orders
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (order == null)
            return null;

        return new GetOrderResponseDto
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            ShippingAddress = order.ShippAddress,
            Status = order.Status.ToString(),
            OrderDate = order.OrderDate,
            RowVersion = Convert.ToBase64String(order.RowVersion),
            Items = order.Items.Select(x => new GetOrderItemDto
            {
                ProductId = x.ProductId,
                ProductName = x.Product.Product_Name,
                Quantity = x.Quantity,
                UnitPrice = x.UnitPrice
            }).ToList()
        };
    }

    public async Task<PagedResultDto<GetOrderResponseDto>> GetOrdersAsync(OrderListRequestDto request)
    {
        var query = _db.Orders
            .Include(x => x.Items)
            .ThenInclude(x => x.Product)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<OrderStatus>(request.Status, true, out var status))
            {
                query = query.Where(x => x.Status == status);
            }
        }

        if (request.CustomerId.HasValue)
            query = query.Where(x => x.CustomerId == request.CustomerId.Value);

        if (request.DateFrom.HasValue)
            query = query.Where(x => x.OrderDate >= request.DateFrom.Value);

        if (request.DateTo.HasValue)
            query = query.Where(x => x.OrderDate <= request.DateTo.Value);

        var total = await query.CountAsync();

        var orders = await query
            .OrderByDescending(x => x.OrderDate)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync();

        return new PagedResultDto<GetOrderResponseDto>
        {
            Page = request.Page,
            PageSize = request.PageSize,
            TotalData = total,
            Data = orders.Select(order => new GetOrderResponseDto
            {
                Id = order.Id,
                CustomerId = order.CustomerId,
                ShippingAddress = order.ShippAddress,
                Status = order.Status.ToString(),
                OrderDate = order.OrderDate,
                Items = order.Items.Select(item => new GetOrderItemDto
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Product_Name,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                }).ToList()
            }).ToList()
        };
    }

    public async Task<GetOrderResponseDto> UpdateStatusAsync(
      long orderId,
      UpdateOrderStatusRequestDto request)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
            throw new AppException("Status tidak valid", 422);

        if (string.IsNullOrWhiteSpace(request.RowVersion))
            throw new AppException("RowVersion wajib dikirim", 400);

        byte[] rowVersion;

        try
        {
            rowVersion = Convert.FromBase64String(request.RowVersion);
        }
        catch
        {
            throw new AppException("RowVersion tidak valid", 400);
        }

        var order = await _db.Orders
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order == null)
            throw new AppException("Order tidak ditemukan", 404);

        if (!IsValidStatusTransition(order.Status, newStatus))
            throw new AppException($"Status {order.Status} tidak bisa diubah ke {newStatus}", 422);

        _db.Entry(order)
            .Property(x => x.RowVersion)
            .OriginalValue = rowVersion;

        order.Status = newStatus;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new AppException(
                "Order sudah diubah oleh user lain. Silakan refresh data.",
                409
            );
        }

        return await GetOrderByIdAsync(order.Id)
            ?? throw new AppException("Order tidak ditemukan", 404);
    }

    private static bool IsValidStatusTransition(
    OrderStatus currentStatus,
    OrderStatus newStatus)
    {
        return currentStatus switch
        {
            OrderStatus.Pending =>
                newStatus == OrderStatus.Confirmed ||
                newStatus == OrderStatus.Cancelled,

            OrderStatus.Confirmed =>
                newStatus == OrderStatus.Shipped ||
                newStatus == OrderStatus.Cancelled,

            OrderStatus.Shipped =>
                newStatus == OrderStatus.Delivered,

            _ => false
        };
    }

    public async Task<GetOrderResponseDto> CancelOrderAsync(long orderId)
    {
        using var transaction = await _db.Database.BeginTransactionAsync();

        var order = await _db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == orderId);

        if (order == null)
            throw new AppException("Order tidak ditemukan", 404);

        if (order.Status != OrderStatus.Pending &&
            order.Status != OrderStatus.Confirmed)
            throw new AppException("Order tidak bisa dicancel", 422);

        var affected = await _db.Database.ExecuteSqlInterpolatedAsync($@"
        UPDATE Orders
        SET Status = {(int)OrderStatus.Cancelled},
            CancelledAt = {DateTime.UtcNow}
        WHERE Id = {orderId}
        AND Status IN ({(int)OrderStatus.Pending}, {(int)OrderStatus.Confirmed})
    ");

        if (affected == 0)
            throw new AppException("Order sudah diproses oleh user lain", 409);

        foreach (var item in order.Items)
        {
            await _db.Database.ExecuteSqlInterpolatedAsync($@"
            UPDATE Products
            SET StockQty = StockQty + {item.Quantity}
            WHERE Id = {item.ProductId}
        ");
        }

        await transaction.CommitAsync();

        return await GetOrderByIdAsync(orderId)
            ?? throw new AppException("Order tidak ditemukan", 404);
    }

    private static string CreateRequestHash(CreateOrderRequestDto request)
    {
        var json = JsonSerializer.Serialize(request);

        var bytes = SHA256.HashData(
            Encoding.UTF8.GetBytes(json)
        );

        return Convert.ToHexString(bytes);
    }

}