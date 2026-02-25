using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;

namespace ThePantry.Application.Commands;

public record MarkAsPurchasedCommand(int Id, int Quantity) : IRequest<bool>;

public class MarkAsPurchasedHandler : IRequestHandler<MarkAsPurchasedCommand, bool>
{
    private readonly ApplicationDbContext _context;
    
    public MarkAsPurchasedHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> Handle(MarkAsPurchasedCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (item == null) return false;
        
        item.OnHandCount += request.Quantity;
        
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
