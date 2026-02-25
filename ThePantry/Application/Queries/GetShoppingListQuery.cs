using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;

namespace ThePantry.Application.Queries;

public record GetShoppingListQuery() : IRequest<List<ShoppingListItemDto>>;

public class ShoppingListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;
    public int OnHandCount { get; set; }
    public int MinimumThreshold { get; set; }
    public int Deficit => MinimumThreshold - OnHandCount;
}

public class GetShoppingListHandler : IRequestHandler<GetShoppingListQuery, List<ShoppingListItemDto>>
{
    private readonly ApplicationDbContext _context;
    
    public GetShoppingListHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<List<ShoppingListItemDto>> Handle(GetShoppingListQuery request, CancellationToken cancellationToken)
    {
        return await _context.InventoryItems
            .Where(i => i.OnHandCount < i.MinimumThreshold)
            .OrderBy(i => i.Category)
            .ThenBy(i => i.Name)
            .Select(i => new ShoppingListItemDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                Category = i.Category,
                OnHandCount = i.OnHandCount,
                MinimumThreshold = i.MinimumThreshold
            })
            .ToListAsync(cancellationToken);
    }
}