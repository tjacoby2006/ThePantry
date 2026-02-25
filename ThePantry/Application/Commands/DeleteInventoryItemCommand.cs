using MediatR;
using ThePantry.Data;

namespace ThePantry.Application.Commands;

public record DeleteInventoryItemCommand(int Id) : IRequest<bool>;

public class DeleteInventoryItemHandler : IRequestHandler<DeleteInventoryItemCommand, bool>
{
    private readonly ApplicationDbContext _context;
    
    public DeleteInventoryItemHandler(ApplicationDbContext context)
    {
        _context = context;
    }
    
    public async Task<bool> Handle(DeleteInventoryItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _context.InventoryItems.FindAsync(new object[] { request.Id }, cancellationToken);
        
        if (item == null) return false;
        
        _context.InventoryItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
