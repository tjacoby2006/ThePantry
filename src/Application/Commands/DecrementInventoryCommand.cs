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
        var item = await _context.InventoryItems.FindAsync(new object[] { request.InventoryItemId }, cancellationToken);
        
        if (item == null)
            return null;
        
        item.OnHandCount = Math.Max(0, item.OnHandCount - request.QuantityUsed);
        item.IsOpened = false;
        item.OpenedDate = null;
        item.LastModifiedDate = DateTime.UtcNow;
        
        // Record usage history
        var usageHistory = new UsageHistory
        {
            InventoryItemId = item.Id,
            QuantityUsed = request.QuantityUsed,
            Timestamp = DateTime.UtcNow
        };
        
        _context.UsageHistories.Add(usageHistory);
        await _context.SaveChangesAsync(cancellationToken);
        
        return item;
    }
}