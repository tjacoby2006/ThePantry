using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Queries;

public record GetInventoryWithUsageHistoryQuery(
    int InventoryItemId
) : IRequest<InventoryWithUsageHistoryResult>;

public class InventoryWithUsageHistoryResult
{
    public InventoryItemDto Item { get; set; } = new();
    public List<UsageHistoryDto> UsageHistory { get; set; } = new();
    public int TotalUsageCount { get; set; }
}

public class UsageHistoryDto
{
    public int Id { get; set; }
    public int QuantityUsed { get; set; }
    public DateTime Timestamp { get; set; }
}

public class GetInventoryWithUsageHistoryHandler : IRequestHandler<GetInventoryWithUsageHistoryQuery, InventoryWithUsageHistoryResult>
{
    private readonly ApplicationDbContext _context;
    
    public GetInventoryWithUsageHistoryHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<InventoryWithUsageHistoryResult> Handle(GetInventoryWithUsageHistoryQuery request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Where(i => i.Id == request.InventoryItemId)
            .Select(i => new InventoryItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                Category = i.Category,
                OnHandCount = i.OnHandCount,
                MinimumThreshold = i.MinimumThreshold,
                Skus = i.Skus.Select(s => s.Sku).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
            
        if (item == null)
        {
            return new InventoryWithUsageHistoryResult();
        }
        
        var usageHistory = await _context.UsageHistories
            .Where(u => u.InventoryItemId == request.InventoryItemId)
            .OrderByDescending(u => u.Timestamp)
            .Select(u => new UsageHistoryDto
            {
                Id = u.Id,
                QuantityUsed = u.QuantityUsed,
                Timestamp = u.Timestamp
            })
            .ToListAsync(cancellationToken);
            
        var totalUsageCount = await _context.UsageHistories
            .Where(u => u.InventoryItemId == request.InventoryItemId)
            .SumAsync(u => u.QuantityUsed, cancellationToken);
        
        return new InventoryWithUsageHistoryResult
        {
            Item = item,
            UsageHistory = usageHistory,
            TotalUsageCount = totalUsageCount
        };
    }
}