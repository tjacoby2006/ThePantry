namespace ThePantry.Domain;

public class InventoryItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = "Pantry";
    public string? ImageUrl { get; set; }
    public int MinimumThreshold { get; set; }
    public int ShelfLifeDays { get; set; } = 30;
    public int UseWithinDays { get; set; } = 7;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? LastModifiedDate { get; set; }

    public int OnHandCount => StockEntries.Count;

    public DateTime ExpiryDate
    {
        get
        {
            if (!StockEntries.Any())
                return CreatedDate.AddDays(ShelfLifeDays);

            return StockEntries.Min(s => s.GetCalculatedExpiryDate());
        }
    }

    public ICollection<StockEntry> StockEntries { get; set; } = new List<StockEntry>();
    public ICollection<ProductSku> Skus { get; set; } = new List<ProductSku>();
    public ICollection<UsageHistory> UsageHistories { get; set; } = new List<UsageHistory>();
}