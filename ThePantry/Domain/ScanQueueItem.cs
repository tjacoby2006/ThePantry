namespace ThePantry.Domain;

public enum ScanStatus
{
    Pending,
    Processing,
    Complete,
    Failed
}

public class ScanQueueItem
{
    public int Id { get; set; }
    public string Upc { get; set; } = string.Empty;
    public string? RawData { get; set; }
    public ScanStatus Status { get; set; } = ScanStatus.Pending;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int? LinkedInventoryItemId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }
    
    public InventoryItem? LinkedInventoryItem { get; set; }
}