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
        var rawItem = await _context.InventoryItems
            .Where(i => i.Id == request.InventoryItemId)
            .Select(i => new
            {
                i.Id,
                i.Name,
                i.Description,
                i.Category,
                i.ImageUrl,
                OnHandCount = i.StockEntries.Count,
                i.MinimumThreshold,
                i.ShelfLifeDays,
                i.UseWithinDays,
                i.CreatedDate,
                Skus = i.Skus.Select(s => s.Sku).ToList(),
                StockEntries = i.StockEntries.Select(s => new
                {
                    s.Id,
                    s.AddedDate,
                    s.IsOpened,
                    s.OpenedDate,
                    s.ExpirationDate
                }).ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);
            
        if (rawItem == null)
        {
            return new InventoryWithUsageHistoryResult();
        }

        var item = new InventoryItemDto
        {
            Id = rawItem.Id,
            Name = rawItem.Name,
            Description = rawItem.Description,
            Category = rawItem.Category,
            ImageUrl = rawItem.ImageUrl,
            OnHandCount = rawItem.OnHandCount,
            MinimumThreshold = rawItem.MinimumThreshold,
            ShelfLifeDays = rawItem.ShelfLifeDays,
            UseWithinDays = rawItem.UseWithinDays,
            IsOpened = rawItem.StockEntries.Any(s => s.IsOpened),
            OpenedDate = rawItem.StockEntries.Where(s => s.IsOpened).Max(s => s.OpenedDate),
            CreatedDate = rawItem.CreatedDate,
            ExpiryDate = rawItem.StockEntries.Any()
                ? rawItem.StockEntries.Min(s => s.ExpirationDate ?? s.AddedDate.AddDays(rawItem.ShelfLifeDays))
                : rawItem.CreatedDate.AddDays(rawItem.ShelfLifeDays),
            Skus = rawItem.Skus,
            StockEntries = rawItem.StockEntries.Select(s => new StockEntryDto
            {
                Id = s.Id,
                AddedDate = s.AddedDate,
                IsOpened = s.IsOpened,
                OpenedDate = s.OpenedDate,
                ExpirationDate = s.ExpirationDate,
                CalculatedExpiryDate = s.ExpirationDate ?? (s.IsOpened && s.OpenedDate.HasValue
                    ? (s.OpenedDate.Value.AddDays(rawItem.UseWithinDays) < s.AddedDate.AddDays(rawItem.ShelfLifeDays)
                        ? s.OpenedDate.Value.AddDays(rawItem.UseWithinDays)
                        : s.AddedDate.AddDays(rawItem.ShelfLifeDays))
                    : s.AddedDate.AddDays(rawItem.ShelfLifeDays))
            }).ToList()
        };
        
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