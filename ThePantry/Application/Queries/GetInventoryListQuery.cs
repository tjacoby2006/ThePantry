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
    public List<string> Skus { get; set; } = new();
    public bool IsBelowThreshold => OnHandCount < MinimumThreshold;
    public int Deficit => Math.Max(0, MinimumThreshold - OnHandCount);
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
                "onhand" => request.SortDescending ? query.OrderByDescending(i => i.OnHandCount) : query.OrderBy(i => i.OnHandCount),
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
            .Select(i => new InventoryItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                Category = i.Category,
                ImageUrl = i.ImageUrl,
                OnHandCount = i.OnHandCount,
                MinimumThreshold = i.MinimumThreshold,
                Skus = i.Skus.Select(s => s.Sku).ToList()
            })
            .ToListAsync(cancellationToken);
        
        return new InventoryListResult
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}