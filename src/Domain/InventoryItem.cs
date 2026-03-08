namespace ThePantry.Domain;

public class InventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Pantry";
    public string? ImageUrl { get; set; }
    public int OnHandCount { get; set; }
    public int MinimumThreshold { get; set; }
    public int ShelfLifeDays { get; set; } = 30;
    public int UseWithinDays { get; set; } = 7;
    public bool IsOpened { get; set; }
    public DateTime? OpenedDate { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedDate { get; set; }

    public DateTime ExpiryDate => CreatedDate.AddDays(ShelfLifeDays);
    
    public ICollection<ProductSku> Skus { get; set; } = new List<ProductSku>();
    public ICollection<UsageHistory> UsageHistories { get; set; } = new List<UsageHistory>();
}