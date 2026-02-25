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
        var item = new InventoryItem
        {
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            OnHandCount = request.OnHandCount,
            MinimumThreshold = request.MinimumThreshold,
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
        
        return item;
    }
}