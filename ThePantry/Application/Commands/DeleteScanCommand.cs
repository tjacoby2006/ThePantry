using MediatR;
using ThePantry.Data;

namespace ThePantry.Application.Commands;

public record DeleteScanCommand(int Id) : IRequest<bool>;

public class DeleteScanHandler : IRequestHandler<DeleteScanCommand, bool>
{
    private readonly ApplicationDbContext _context;
    
    public DeleteScanHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> Handle(DeleteScanCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.ScanQueueItems.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (item == null) return false;
        
        _context.ScanQueueItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
