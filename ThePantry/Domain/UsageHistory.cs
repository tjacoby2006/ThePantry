namespace ThePantry.Domain;

public class UsageHistory
{
    public int Id { get; set; }
    public int InventoryItemId { get; set; }
    public int QuantityUsed { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    
    public InventoryItem? InventoryItem { get; set; }
}