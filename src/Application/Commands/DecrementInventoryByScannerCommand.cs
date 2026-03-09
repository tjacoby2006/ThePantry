using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Services;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record DecrementInventoryByScannerCommand(
    string Upc,
    int QuantityUsed = 1
) : IRequest<InventoryItem?>;

public class DecrementInventoryByScannerHandler : IRequestHandler<DecrementInventoryByScannerCommand, InventoryItem?>
{
    private readonly ApplicationDbContext _context;
    private readonly IProductLookupService _productLookupService;
    
    public DecrementInventoryByScannerHandler(ApplicationDbContext context, IProductLookupService productLookupService)
    {
        _context = context;
        _productLookupService = productLookupService;
    }
    
    public async Task<InventoryItem?> Handle(DecrementInventoryByScannerCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Skus)
            .Include(i => i.StockEntries)
            .FirstOrDefaultAsync(i => i.Skus.Any(s => s.Sku == request.Upc), cancellationToken);
        
        if (item == null)
        {
            // Create a new item with 0 on hand if not found
            var lookupResult = await _productLookupService.LookupAsync(request.Upc, cancellationToken);
            
            item = new InventoryItem
            {
                Name = lookupResult?.Name ?? $"Unknown Product ({request.Upc})",
                Description = lookupResult?.Description ?? lookupResult?.Brand,
                ImageUrl = lookupResult?.ImageUrl,
                Category = "Uncategorized",
                MinimumThreshold = 1,
                CreatedDate = DateTime.UtcNow
            };
            
            item.Skus.Add(new ProductSku { Sku = request.Upc });
            _context.InventoryItems.Add(item);
            await _context.SaveChangesAsync(cancellationToken);
        }
        
        // FIFO logic: remove oldest entries first
        var entriesToRemove = item.StockEntries
            .OrderBy(s => s.AddedDate)
            .Take(request.QuantityUsed)
            .ToList();

        foreach (var entry in entriesToRemove)
        {
            _context.StockEntries.Remove(entry);
        }

        item.LastModifiedDate = DateTime.UtcNow;
        
        // Record usage history
        var usageHistory = new UsageHistory
        {
            InventoryItemId = item.Id,
            QuantityUsed = entriesToRemove.Count,
            Timestamp = DateTime.UtcNow
        };
        
        _context.UsageHistories.Add(usageHistory);
        await _context.SaveChangesAsync(cancellationToken);
        
        return item;
    }
}