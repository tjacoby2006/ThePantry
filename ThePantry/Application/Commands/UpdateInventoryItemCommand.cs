using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record UpdateInventoryItemCommand(
    int Id,
    string Name,
    string? Description,
    string Category,
    int OnHandCount,
    int MinimumThreshold,
    string? ImageUrl = null,
    List<string>? Skus = null
) : IRequest<bool>;

public class UpdateInventoryItemHandler : IRequestHandler<UpdateInventoryItemCommand, bool>
{
    private readonly ApplicationDbContext _context;
    
    public UpdateInventoryItemHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> Handle(UpdateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Skus)
            .FirstOrDefaultAsync(i => i.Id == request.Id, cancellationToken);
        
        if (item == null) return false;
        
        item.Name = request.Name;
        item.Description = request.Description;
        item.Category = request.Category;
        item.ImageUrl = request.ImageUrl;
        item.OnHandCount = request.OnHandCount;
        item.MinimumThreshold = request.MinimumThreshold;
        item.LastModifiedDate = DateTime.UtcNow;

        // Update SKUs
        if (request.Skus != null)
        {
            var newSkus = request.Skus.Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
            
            // Remove SKUs that are no longer present
            var skusToRemove = item.Skus.Where(s => !newSkus.Contains(s.Sku)).ToList();
            foreach (var sku in skusToRemove)
            {
                item.Skus.Remove(sku);
            }

            // Add new SKUs
            var existingSkuStrings = item.Skus.Select(s => s.Sku).ToList();
            foreach (var skuStr in newSkus)
            {
                if (!existingSkuStrings.Contains(skuStr))
                {
                    item.Skus.Add(new ProductSku { Sku = skuStr });
                }
            }
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
