using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Queries;

public record GetShoppingListQuery() : IRequest<List<InventoryItem>>;

public class GetShoppingListHandler : IRequestHandler<GetShoppingListQuery, List<InventoryItem>>
{
    private readonly ApplicationDbContext _context;
    
    public GetShoppingListHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<InventoryItem>> Handle(GetShoppingListQuery request, CancellationToken cancellationToken)
    {
        return await _context.InventoryItems
            .Where(i => i.OnHandCount < i.MinimumThreshold)
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }
}