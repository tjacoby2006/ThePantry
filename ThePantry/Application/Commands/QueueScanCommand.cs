using MediatR;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record QueueScanCommand(string UPC) : IRequest<ScanQueueItem>;

public class QueueScanHandler : IRequestHandler<QueueScanCommand, ScanQueueItem>
{
    private readonly ApplicationDbContext _context;
    
    public QueueScanHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<ScanQueueItem> Handle(QueueScanCommand request, CancellationToken cancellationToken)
    {
        var scanItem = new ScanQueueItem
        {
            UPC = request.UPC,
            Status = ScanStatus.Pending,
            Timestamp = DateTime.UtcNow
        };
        
        _context.ScanQueueItems.Add(scanItem);
        await _context.SaveChangesAsync(cancellationToken);
        
        return scanItem;
    }
}