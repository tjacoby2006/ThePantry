using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Services;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record ResyncInventoryCommand() : IRequest<int>;

public class ResyncInventoryHandler : IRequestHandler<ResyncInventoryCommand, int>
{
    private readonly ApplicationDbContext _context;
    private readonly IProductLookupService _productLookupService;

    public ResyncInventoryHandler(ApplicationDbContext context, IProductLookupService productLookupService)
    {
        _context = context;
        _productLookupService = productLookupService;
    }

    public async Task<int> Handle(ResyncInventoryCommand request, CancellationToken cancellationToken)
    {
        var items = await _context.InventoryItems
            .Include(i => i.Skus)
            .ToListAsync(cancellationToken);

        int updatedCount = 0;

        foreach (var item in items)
        {
            // Try to find a UPC to lookup. We'll use the first SKU available.
            var upc = item.Skus.FirstOrDefault()?.Sku;
            if (string.IsNullOrEmpty(upc)) continue;

            var result = await _productLookupService.LookupAsync(upc, cancellationToken);
            if (result != null)
            {
                bool changed = false;

                if (item.ImageUrl != result.ImageUrl && !string.IsNullOrWhiteSpace(result.ImageUrl))
                {
                    item.ImageUrl = result.ImageUrl;
                    changed = true;
                }

                if (item.Description != result.Description && !string.IsNullOrWhiteSpace(result.Description))
                {
                    item.Description = result.Description;
                    changed = true;
                }

                // Update name if it's currently generic, empty, or different from the result
                if (string.IsNullOrWhiteSpace(item.Name) || 
                    item.Name.StartsWith("Unknown Product", StringComparison.OrdinalIgnoreCase) ||
                    (item.Name != result.Name && !string.IsNullOrWhiteSpace(result.Name)))
                {
                    item.Name = result.Name;
                    changed = true;
                }

                if (changed)
                {
                    item.LastModifiedDate = DateTime.UtcNow;
                    updatedCount++;
                }
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return updatedCount;
    }
}
