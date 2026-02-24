using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Queries;

public record GetInventoryListQuery(
    string? SearchTerm = null,
    string? Category = null,
    bool? BelowThresholdOnly = false
) : IRequest<List<InventoryItem>>;

public class GetInventoryListHandler : IRequestHandler<GetInventoryListQuery, List<InventoryItem>>
{
    private readonly ApplicationDbContext _context;
    
    public GetInventoryListHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<InventoryItem>> Handle(GetInventoryListQuery request, CancellationToken cancellationToken)
    {
        var query = _context.InventoryItems.AsQueryable();
        
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var search = request.SearchTerm.ToLower();
            query = query.Where(i => 
                i.Name.ToLower().Contains(search) || 
                (i.Description != null && i.Description.ToLower().Contains(search)));
        }
        
        if (!string.IsNullOrWhiteSpace(request.Category))
        {
            query = query.Where(i => i.Category == request.Category);
        }
        
        if (request.BelowThresholdOnly == true)
        {
            query = query.Where(i => i.OnHandCount < i.MinimumThreshold);
        }
        
        return await query
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }
}