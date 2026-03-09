using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record IncrementInventoryCommand(
    int InventoryItemId,
    int QuantityAdded = 1
) : IRequest<InventoryItem?>;

public class IncrementInventoryHandler : IRequestHandler<IncrementInventoryCommand, InventoryItem?>
{
    private readonly ApplicationDbContext _context;
    
    public IncrementInventoryHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<InventoryItem?> Handle(IncrementInventoryCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Include(i => i.StockEntries)
            .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, cancellationToken);
        
        if (item == null)
            return null;
        
        for (int i = 0; i < request.QuantityAdded; i++)
        {
            _context.StockEntries.Add(new StockEntry { InventoryItemId = item.Id });
        }

        item.LastModifiedDate = DateTime.UtcNow;
        
        await _context.SaveChangesAsync(cancellationToken);
        
        return item;
    }
}