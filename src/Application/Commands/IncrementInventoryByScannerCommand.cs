using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Services;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record IncrementInventoryByScannerCommand(
    string Upc,
    int QuantityToAdd = 1
) : IRequest<InventoryItem>;

public class IncrementInventoryByScannerHandler : IRequestHandler<IncrementInventoryByScannerCommand, InventoryItem>
{
    private readonly ApplicationDbContext _context;
    private readonly IProductLookupService _productLookupService;
    
    public IncrementInventoryByScannerHandler(ApplicationDbContext context, IProductLookupService productLookupService)
    {
        _context = context;
        _productLookupService = productLookupService;
    }
    
    public async Task<InventoryItem> Handle(IncrementInventoryByScannerCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Skus)
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
                OnHandCount = request.QuantityToAdd,
                MinimumThreshold = 1,
                CreatedDate = DateTime.UtcNow
            };
            
            item.Skus.Add(new ProductSku { Sku = request.Upc });
            _context.InventoryItems.Add(item);
        }
        else
        {
            item.OnHandCount += request.QuantityToAdd;
            item.LastModifiedDate = DateTime.UtcNow;
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        
        return item;
    }
}