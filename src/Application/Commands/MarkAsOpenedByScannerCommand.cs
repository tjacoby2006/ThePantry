using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Services;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record MarkAsOpenedByScannerCommand(string Upc) : IRequest<InventoryItem?>;

public class MarkAsOpenedByScannerHandler : IRequestHandler<MarkAsOpenedByScannerCommand, InventoryItem?>
{
    private readonly ApplicationDbContext _context;
    private readonly IProductLookupService _productLookupService;

    public MarkAsOpenedByScannerHandler(ApplicationDbContext context, IProductLookupService productLookupService)
    {
        _context = context;
        _productLookupService = productLookupService;
    }

    public async Task<InventoryItem?> Handle(MarkAsOpenedByScannerCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Skus)
            .Include(i => i.StockEntries)
            .FirstOrDefaultAsync(i => i.Skus.Any(s => s.Sku == request.Upc), cancellationToken);

        if (item == null)
        {
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

            // Add initial stock entry
            var entry = new StockEntry { InventoryItemId = item.Id, IsOpened = true, OpenedDate = DateTime.UtcNow };
            _context.StockEntries.Add(entry);
        }
        else
        {
            // Mark oldest unopened entry as opened
            var entryToOpen = item.StockEntries
                .OrderBy(s => s.IsOpened)
                .ThenBy(s => s.AddedDate)
                .FirstOrDefault();

            if (entryToOpen != null)
            {
                entryToOpen.IsOpened = true;
                entryToOpen.OpenedDate = DateTime.UtcNow;
            }
            else
            {
                // If no entries exist, create one
                _context.StockEntries.Add(new StockEntry { InventoryItemId = item.Id, IsOpened = true, OpenedDate = DateTime.UtcNow });
            }
        }

        item.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return item;
    }
}