using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record DecrementInventoryCommand(
    int InventoryItemId,
    int QuantityUsed = 1
) : IRequest<InventoryItem?>;

public class DecrementInventoryHandler : IRequestHandler<DecrementInventoryCommand, InventoryItem?>
{
    private readonly ApplicationDbContext _context;
    
    public DecrementInventoryHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<InventoryItem?> Handle(DecrementInventoryCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Include(i => i.StockEntries)
            .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, cancellationToken);
        
        if (item == null)
            return null;
        
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