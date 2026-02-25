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
        // Check for recent duplicate scans (within last 5 seconds) to prevent double-queuing
        var recentScan = await _context.ScanQueueItems
            .Where(s => s.Upc == request.Upc && s.Timestamp > DateTime.UtcNow.AddSeconds(-5))
            .FirstOrDefaultAsync(cancellationToken);

        if (recentScan != null)
        {
            return recentScan;
        }

        // Check if we already have this UPC in inventory
        var existingItem = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Skus.Any(s => s.Sku == request.Upc), cancellationToken);
        
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