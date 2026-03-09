using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record CombineInventoryItemsCommand(List<int> ItemIds) : IRequest<bool>;

public class CombineInventoryItemsHandler : IRequestHandler<CombineInventoryItemsCommand, bool>
{
    private readonly ApplicationDbContext _context;

    public CombineInventoryItemsHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(CombineInventoryItemsCommand request, CancellationToken cancellationToken)
    {
        if (request.ItemIds == null || request.ItemIds.Count < 2)
        {
            return false;
        }

        var items = await _context.InventoryItems
            .Include(i => i.Skus)
            .Include(i => i.UsageHistories)
            .Include(i => i.StockEntries)
            .Where(i => request.ItemIds.Contains(i.Id))
            .ToListAsync(cancellationToken);

        if (items.Count < 2)
        {
            return false;
        }

        // Update ScanQueueItems that might be linked to the items being removed
        var scanQueueItems = await _context.ScanQueueItems
            .Where(s => s.LinkedInventoryItemId != null && request.ItemIds.Contains(s.LinkedInventoryItemId.Value))
            .ToListAsync(cancellationToken);

        // The first item in the list (based on the order of IDs provided, or just the first one found)
        // will be the primary item. The user request says "The name of the combined inventory item 
        // should be of the first item selected in the grid."
        // Since we want to preserve the order of selection, we should find the item that matches the first ID.
        var primaryItemId = request.ItemIds.First();
        var primaryItem = items.FirstOrDefault(i => i.Id == primaryItemId);
        
        if (primaryItem == null)
        {
            // Fallback to the first item in the list if the specific ID isn't found for some reason
            primaryItem = items.First();
        }

        primaryItem.ImageUrl = string.IsNullOrWhiteSpace(primaryItem.ImageUrl) ? null : primaryItem.ImageUrl;
        primaryItem.ImageUrl ??= items.Select(i => i.ImageUrl).FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

        var otherItems = items.Where(i => i.Id != primaryItem.Id).ToList();

        // Update ScanQueueItems to point to the primary item
        foreach (var scanItem in scanQueueItems)
        {
            scanItem.LinkedInventoryItemId = primaryItem.Id;
        }

        foreach (var otherItem in otherItems)
        {
            // Move StockEntries
            foreach (var entry in otherItem.StockEntries.ToList())
            {
                entry.InventoryItemId = primaryItem.Id;
                primaryItem.StockEntries.Add(entry);
            }

            // Move SKUs
            foreach (var sku in otherItem.Skus.ToList())
            {
                // Check if SKU already exists in primary item to avoid duplicates
                if (!primaryItem.Skus.Any(s => s.Sku == sku.Sku))
                {
                    sku.InventoryItemId = primaryItem.Id;
                    primaryItem.Skus.Add(sku);
                }
                else
                {
                    // If it exists, we can just remove it from the other item (it will be deleted when otherItem is deleted)
                    _context.Remove(sku);
                }
            }

            // Move Usage History
            foreach (var history in otherItem.UsageHistories.ToList())
            {
                history.InventoryItemId = primaryItem.Id;
                primaryItem.UsageHistories.Add(history);
            }

            // Remove the other item
            _context.InventoryItems.Remove(otherItem);
        }

        primaryItem.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
