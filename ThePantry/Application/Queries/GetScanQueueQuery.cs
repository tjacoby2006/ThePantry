using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Queries;

public record GetScanQueueQuery(
    ScanStatus? StatusFilter = null
) : IRequest<List<ScanQueueItemDto>>;

public class ScanQueueItemDto
{
    public int Id { get; set; }
    public string Upc { get; set; } = string.Empty;
    public string? RawData { get; set; }
    public ScanStatus Status { get; set; }
    public DateTime Timestamp { get; set; }
    public int? LinkedInventoryItemId { get; set; }
    public string? ProductName { get; set; }
    public string? ProductDescription { get; set; }
    public string? ImagePath { get; set; }
}

public class GetScanQueueHandler : IRequestHandler<GetScanQueueQuery, List<ScanQueueItemDto>>
{
    private readonly ApplicationDbContext _context;
    
    public GetScanQueueHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<ScanQueueItemDto>> Handle(GetScanQueueQuery request, CancellationToken cancellationToken)
    {
        var query = _context.ScanQueueItems.AsQueryable();
        
        if (request.StatusFilter.HasValue)
        {
            query = query.Where(s => s.Status == request.StatusFilter.Value);
        }
        
        return await query
            .OrderByDescending(s => s.Timestamp)
            .Select(s => new ScanQueueItemDto
            {
                Id = s.Id,
                Upc = s.Upc,
                RawData = s.RawData,
                Status = s.Status,
                Timestamp = s.Timestamp,
                LinkedInventoryItemId = s.LinkedInventoryItemId,
                ProductName = s.ProductName,
                ProductDescription = s.ProductDescription,
                ImagePath = s.ImagePath
            })
            .ToListAsync(cancellationToken);
    }
}