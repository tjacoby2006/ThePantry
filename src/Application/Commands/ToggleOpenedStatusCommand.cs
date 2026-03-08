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
        var item = await _context.InventoryItems.FindAsync(new object[] { request.InventoryItemId }, cancellationToken);

        if (item == null)
            return null;

        item.IsOpened = !item.IsOpened;
        item.OpenedDate = item.IsOpened ? DateTime.UtcNow : null;
        item.LastModifiedDate = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return item;
    }
}