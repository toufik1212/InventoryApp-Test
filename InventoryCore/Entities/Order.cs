using InventoryCore.Enums;

namespace InventoryCore.Entities;

public class Order
{
    public long Id { get; set; }
    public int CustomerId { get; set; }
    public string ShippAddress { get; set; } = string.Empty;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public byte[] RowVersion { get; set; } = [];
}