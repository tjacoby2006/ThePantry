using MediatR;
using ThePantry.Data;

namespace ThePantry.Application.Commands;

public record DeleteInventoryItemCommand(int Id) : IRequest<bool>;

public class DeleteInventoryItemHandler : IRequestHandler<DeleteInventoryItemCommand, bool>
{
    private readonly ApplicationDbContext _context;
    
    public DeleteInventoryItemHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> Handle(DeleteInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (item == null) return false;

        // Remove related usage history
        var usageHistory = _context.UsageHistories.Where(u => u.InventoryItemId == request.Id);
        _context.UsageHistories.RemoveRange(usageHistory);

        // Unlink scan queue items
        var scanItems = _context.ScanQueueItems.Where(s => s.LinkedInventoryItemId == request.Id);
        foreach (var scan in scanItems)
        {
            scan.LinkedInventoryItemId = null;
        }
        
        _context.InventoryItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
