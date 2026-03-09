using System;

namespace ThePantry.Domain;

public class StockEntry
{
    public int Id { get; set; }
    public int InventoryItemId { get; set; }
    public DateTime AddedDate { get; set; } = DateTime.UtcNow;
    public bool IsOpened { get; set; }
    public DateTime? OpenedDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    // Navigation property
    public InventoryItem? InventoryItem { get; set; }

    public DateTime GetCalculatedExpiryDate()
    {
        if (ExpirationDate.HasValue)
            return ExpirationDate.Value;

        if (InventoryItem == null)
            return AddedDate.AddDays(30); // Default fallback

        if (IsOpened && OpenedDate.HasValue)
        {
            var openedExpiry = OpenedDate.Value.AddDays(InventoryItem.UseWithinDays);
            var shelfLifeExpiry = AddedDate.AddDays(InventoryItem.ShelfLifeDays);
            return openedExpiry < shelfLifeExpiry ? openedExpiry : shelfLifeExpiry;
        }

        return AddedDate.AddDays(InventoryItem.ShelfLifeDays);
    }
}
