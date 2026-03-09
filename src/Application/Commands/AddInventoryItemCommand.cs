using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record AddInventoryItemCommand(
    string Name,
    string? Description,
    string Category,
    int OnHandCount,
    int MinimumThreshold,
    int ShelfLifeDays = 30,
    int UseWithinDays = 7,
    string? ImageUrl = null,
    List<string>? Skus = null
) : IRequest<InventoryItem>;

public class AddInventoryItemHandler : IRequestHandler<AddInventoryItemCommand, InventoryItem>
{
    private readonly ApplicationDbContext _context;
    
    public AddInventoryItemHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<InventoryItem> Handle(AddInventoryItemCommand request, CancellationToken cancellationToken)
    {
        // Check if an item with the same name already exists
        var existingItem = await _context.InventoryItems
            .Include(i => i.Skus)
            .FirstOrDefaultAsync(i => i.Name.ToLower() == request.Name.ToLower(), cancellationToken);

        if (existingItem != null)
        {
            // Update existing item instead of creating a new one
            for (int i = 0; i < request.OnHandCount; i++)
            {
                _context.StockEntries.Add(new StockEntry { InventoryItemId = existingItem.Id });
            }
            
            if (string.IsNullOrEmpty(existingItem.ImageUrl) && !string.IsNullOrEmpty(request.ImageUrl))
            {
                existingItem.ImageUrl = request.ImageUrl;
            }

            if (request.Skus != null)
            {
                foreach (var sku in request.Skus.Where(s => !string.IsNullOrWhiteSpace(s)))
                {
                    if (!existingItem.Skus.Any(s => s.Sku == sku))
                    {
                        existingItem.Skus.Add(new ProductSku { Sku = sku });
                    }
                }
            }
            
            await _context.SaveChangesAsync(cancellationToken);
            return existingItem;
        }

        var item = new InventoryItem
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl,
            MinimumThreshold = request.MinimumThreshold,
            ShelfLifeDays = request.ShelfLifeDays,
            UseWithinDays = request.UseWithinDays,
            CreatedDate = DateTime.UtcNow
        };

        if (request.Skus != null && request.Skus.Any())
        {
            foreach (var sku in request.Skus.Where(s => !string.IsNullOrWhiteSpace(s)))
            {
                item.Skus.Add(new ProductSku { Sku = sku });
            }
        }
        
        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        // Add stock entries after item is created to get the ID
        for (int i = 0; i < request.OnHandCount; i++)
        {
            _context.StockEntries.Add(new StockEntry { InventoryItemId = item.Id });
        }
        await _context.SaveChangesAsync(cancellationToken);
        
        return item;
    }
}