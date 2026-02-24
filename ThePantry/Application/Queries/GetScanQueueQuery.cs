using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Queries;

public record GetScanQueueQuery(
    ScanStatus? StatusFilter = null
) : IRequest<List<ScanQueueItem>>;

public class GetScanQueueHandler : IRequestHandler<GetScanQueueQuery, List<ScanQueueItem>>
{
    private readonly ApplicationDbContext _context;
    
    public GetScanQueueHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<ScanQueueItem>> Handle(GetScanQueueQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ScanQueueItems
            .Include(s => s.LinkedInventoryItem)
            .AsQueryable();
        
        if (request.StatusFilter.HasValue)
        {
            query = query.Where(s => s.Status == request.StatusFilter.Value);
        }
        
        return await query
            .OrderByDescending(s => s.Timestamp)
            .ToListAsync(cancellationToken);
    }
}