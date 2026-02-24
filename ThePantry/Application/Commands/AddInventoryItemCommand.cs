using MediatR;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record AddInventoryItemCommand(
    string Name,
    string? Description,
    string Category,
    int OnHandCount,
    int MinimumThreshold,
    string? UPC
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
            UPC = request.UPC,
            CreatedDate = DateTime.UtcNow
        };
        
        _context.InventoryItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);
        
        return item;
    }
}