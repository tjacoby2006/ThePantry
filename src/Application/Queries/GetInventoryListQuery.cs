using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Queries;

public record GetInventoryListQuery(
    string? SearchTerm = null,
    string? Category = null,
    int Page = 1,
    int PageSize = 20,
    string? SortBy = null,
    bool SortDescending = false
) : IRequest<InventoryListResult>;

public class InventoryListResult
{
    public List<InventoryItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class InventoryItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public int OnHandCount { get; set; }
    public int MinimumThreshold { get; set; }
    public int ShelfLifeDays { get; set; }
    public int UseWithinDays { get; set; }
    public bool IsOpened { get; set; }
    public DateTime? OpenedDate { get; set; }
    public DateTime CreatedDate { get; set; }
    public List<string> Skus { get; set; } = new();
    public bool IsBelowThreshold => OnHandCount < MinimumThreshold;
    public int Deficit => Math.Max(0, MinimumThreshold - OnHandCount);
    public DateTime ExpiryDate { get; set; }
    public List<StockEntryDto> StockEntries { get; set; } = new();
}

public class StockEntryDto
{
    public int Id { get; set; }
    public DateTime AddedDate { get; set; }
    public bool IsOpened { get; set; }
    public DateTime? OpenedDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DateTime CalculatedExpiryDate { get; set; }
}

public class GetInventoryListHandler : IRequestHandler<GetInventoryListQuery, InventoryListResult>
{
    private readonly ApplicationDbContext _context;
    
    public GetInventoryListHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<InventoryListResult> Handle(GetInventoryListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InventoryItems.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(i => 
                i.Name.ToLower().Contains(search) || 
                (i.Description != null && i.Description.ToLower().Contains(search)) ||
                i.Skus.Any(s => s.Sku.ToLower().Contains(search)));
        }
        
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(i => i.Category == request.Category);
        }
        
        var totalCount = await query.CountAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.SortBy))
        {
            query = request.SortBy.ToLower() switch
            {
                "name" => request.SortDescending ? query.OrderByDescending(i => i.Name) : query.OrderBy(i => i.Name),
                "category" => request.SortDescending ? query.OrderByDescending(i => i.Category) : query.OrderBy(i => i.Category),
                "onhand" => request.SortDescending ? query.OrderByDescending(i => i.StockEntries.Count) : query.OrderBy(i => i.StockEntries.Count),
                "min" => request.SortDescending ? query.OrderByDescending(i => i.MinimumThreshold) : query.OrderBy(i => i.MinimumThreshold),
                _ => query.OrderBy(i => i.Name)
            };
        }
        else
        {
            query = query.OrderBy(i => i.Name);
        }
        
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
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
            .ToListAsync(cancellationToken);

        var resultItems = items.Select(i => new InventoryItemDto
        {
            Id = i.Id,
            Name = i.Name,
            Description = i.Description,
            Category = i.Category,
            ImageUrl = i.ImageUrl,
            OnHandCount = i.OnHandCount,
            MinimumThreshold = i.MinimumThreshold,
            ShelfLifeDays = i.ShelfLifeDays,
            UseWithinDays = i.UseWithinDays,
            IsOpened = i.StockEntries.Any(s => s.IsOpened),
            OpenedDate = i.StockEntries.Where(s => s.IsOpened).Max(s => s.OpenedDate),
            CreatedDate = i.CreatedDate,
            ExpiryDate = i.StockEntries.Any()
                ? i.StockEntries.Min(s => s.ExpirationDate ?? s.AddedDate.AddDays(i.ShelfLifeDays))
                : i.CreatedDate.AddDays(i.ShelfLifeDays),
            Skus = i.Skus,
            StockEntries = i.StockEntries.Select(s => new StockEntryDto
            {
                Id = s.Id,
                AddedDate = s.AddedDate,
                IsOpened = s.IsOpened,
                OpenedDate = s.OpenedDate,
                ExpirationDate = s.ExpirationDate,
                CalculatedExpiryDate = s.ExpirationDate ?? (s.IsOpened && s.OpenedDate.HasValue
                    ? (s.OpenedDate.Value.AddDays(i.UseWithinDays) < s.AddedDate.AddDays(i.ShelfLifeDays)
                        ? s.OpenedDate.Value.AddDays(i.UseWithinDays)
                        : s.AddedDate.AddDays(i.ShelfLifeDays))
                    : s.AddedDate.AddDays(i.ShelfLifeDays))
            }).ToList()
        }).ToList();
        
        return new InventoryListResult
        {
            Items = resultItems,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}