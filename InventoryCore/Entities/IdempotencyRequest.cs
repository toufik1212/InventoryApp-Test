namespace InventoryCore.Entities;

public class IdempotencyRequest
{
    public long Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string RequestHash { get; set; } = string.Empty;
    public long? OrderId { get; set; }
    public string Status { get; set; } = "Processing";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}