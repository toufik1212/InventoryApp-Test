namespace InventoryCore.Entities;

public class Product
{
    public int Id { get; set; }
    public string Product_Name { get; set; } = string.Empty;
    public int StockQty { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}