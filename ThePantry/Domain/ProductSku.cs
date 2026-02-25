namespace ThePantry.Domain;

public class ProductSku
{
    public int Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public int InventoryItemId { get; set; }
    public InventoryItem? InventoryItem { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
