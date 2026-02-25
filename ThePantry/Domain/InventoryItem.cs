namespace ThePantry.Domain;

public class InventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Pantry";
    public int OnHandCount { get; set; }
    public int MinimumThreshold { get; set; }
    public string? Upc { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedDate { get; set; }
    
    public ICollection<UsageHistory> UsageHistories { get; set; } = new List<UsageHistory>();
}