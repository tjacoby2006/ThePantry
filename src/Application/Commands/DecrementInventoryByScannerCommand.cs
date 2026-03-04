using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record DecrementInventoryByScannerCommand(
    string Upc,
    int QuantityUsed = 1
) : IRequest<InventoryItem?>;

public class DecrementInventoryByScannerHandler : IRequestHandler<DecrementInventoryByScannerCommand, InventoryItem?>
{
    private readonly ApplicationDbContext _context;
    
    public DecrementInventoryByScannerHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<InventoryItem?> Handle(DecrementInventoryByScannerCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems
            .Include(i => i.Skus)
            .FirstOrDefaultAsync(i => i.Skus.Any(s => s.Sku == request.Upc), cancellationToken);
        
        if (item == null)
            return null;
        
        item.OnHandCount = Math.Max(0, item.OnHandCount - request.QuantityUsed);
        item.LastModifiedDate = DateTime.UtcNow;
        
        // Record usage history
        var usageHistory = new UsageHistory
        {
            InventoryItemId = item.Id,
            QuantityUsed = request.QuantityUsed,
            Timestamp = DateTime.UtcNow
        };
        
        _context.UsageHistories.Add(usageHistory);
        await _context.SaveChangesAsync(cancellationToken);
        
        return item;
    }
}