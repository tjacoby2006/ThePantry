using Microsoft.EntityFrameworkCore;
using MediatR;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record ToggleOpenedStatusCommand(int InventoryItemId) : IRequest<InventoryItem?>;

public class ToggleOpenedStatusHandler : IRequestHandler<ToggleOpenedStatusCommand, InventoryItem?>
{
    private readonly ApplicationDbContext _context;

    public ToggleOpenedStatusHandler(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryItem?> Handle(ToggleOpenedStatusCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Include(i => i.StockEntries)
            .FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, cancellationToken);

        if (item == null || !item.StockEntries.Any())
            return null;

        // If any items are opened, we want to "close" the most recently opened one.
        // Otherwise, we want to "open" the oldest unopened one.
        var entryToToggle = item.StockEntries.Any(s => s.IsOpened)
            ? item.StockEntries
                .Where(s => s.IsOpened)
                .OrderByDescending(s => s.OpenedDate) // Most recently opened first
                .FirstOrDefault()
            : item.StockEntries
                .Where(s => !s.IsOpened)
                .OrderBy(s => s.AddedDate) // Oldest unopened first
                .FirstOrDefault();

        if (entryToToggle != null)
        {
            entryToToggle.IsOpened = !entryToToggle.IsOpened;
            entryToToggle.OpenedDate = entryToToggle.IsOpened ? DateTime.UtcNow : null;
        }

        item.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return item;
    }
}