using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;

namespace ThePantry.Application.Commands;

public record UpdateInventoryItemCommand(
    int Id,
    string Name,
    string? Description,
    string Category,
    int OnHandCount,
    int MinimumThreshold,
    string? Upc = null
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
        var item = await _context.InventoryItems.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (item == null) return false;
        
        item.Name = request.Name;
        item.Description = request.Description;
        item.Category = request.Category;
        item.OnHandCount = request.OnHandCount;
        item.MinimumThreshold = request.MinimumThreshold;
        item.Upc = request.Upc;
        
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
