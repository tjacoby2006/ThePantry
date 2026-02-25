using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record QueueScanCommand(
    string Upc,
    string? RawData = null
) : IRequest<ScanQueueItem>;

public class QueueScanHandler : IRequestHandler<QueueScanCommand, ScanQueueItem>
{
    private readonly ApplicationDbContext _context;
    
    public QueueScanHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<ScanQueueItem> Handle(QueueScanCommand request, CancellationToken cancellationToken)
    {
        // Check if we already have this UPC in inventory
        var existingItem = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Upc == request.Upc, cancellationToken);
        
        var scanItem = new ScanQueueItem
        {
            Upc = request.Upc,
            RawData = request.RawData,
            Status = ScanStatus.Pending,
            Timestamp = DateTime.UtcNow,
            LinkedInventoryItemId = existingItem?.Id
        };
        
        _context.ScanQueueItems.Add(scanItem);
        await _context.SaveChangesAsync(cancellationToken);
        
        return scanItem;
    }
}