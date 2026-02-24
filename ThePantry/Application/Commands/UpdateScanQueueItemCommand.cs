using MediatR;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record UpdateScanQueueItemCommand(
    int ScanQueueItemId,
    ScanStatus NewStatus,
    int? LinkedInventoryItemId = null
) : IRequest<bool>;

public class UpdateScanQueueItemHandler : IRequestHandler<UpdateScanQueueItemCommand, bool>
{
    private readonly ApplicationDbContext _context;
    
    public UpdateScanQueueItemHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> Handle(UpdateScanQueueItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.ScanQueueItems.FindAsync(new object[] { request.ScanQueueItemId }, cancellationToken);
        
        if (item == null)
            return false;
        
        item.Status = request.NewStatus;
        if (request.LinkedInventoryItemId.HasValue)
        {
            item.LinkedInventoryItemId = request.LinkedInventoryItemId;
        }
        
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}