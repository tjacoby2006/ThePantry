using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Services;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record ProcessScanCommand(int ScanQueueItemId) : IRequest<ScanQueueItem?>;

public class ProcessScanHandler : IRequestHandler<ProcessScanCommand, ScanQueueItem?>
{
    private readonly ApplicationDbContext _context;
    private readonly IProductLookupService _lookupService;
    
    public ProcessScanHandler(ApplicationDbContext context, IProductLookupService lookupService)
    {
        _context = context;
        _lookupService = lookupService;
    }
    
    public async Task<ScanQueueItem?> Handle(ProcessScanCommand request, CancellationToken cancellationToken)
    {
        var scanItem = await _context.ScanQueueItems.FindAsync(new object[] { request.ScanQueueItemId }, cancellationToken);
        
        if (scanItem == null)
            return null;
        
        // Mark as processing
        scanItem.Status = ScanStatus.Processing;
        await _context.SaveChangesAsync(cancellationToken);
        
        // Look up product
        var result = await _lookupService.LookupAsync(scanItem.UPC, cancellationToken);
        
        if (result?.Success == true)
        {
            // Product found - create inventory item
            var inventoryItem = new InventoryItem
            {
                Name = result.Name ?? "Unknown Product",
                Description = result.Description,
                Category = "Pantry",
                OnHandCount = 1,
                MinimumThreshold = 1,
                UPC = scanItem.UPC,
                CreatedDate = DateTime.UtcNow
            };
            
            _context.InventoryItems.Add(inventoryItem);
            
            // Update scan item
            scanItem.Status = ScanStatus.Complete;
            scanItem.LinkedInventoryItemId = inventoryItem.Id;
            scanItem.ProductName = result.Name;
            scanItem.ProductDescription = result.Description;
            
            await _context.SaveChangesAsync(cancellationToken);
            
            return scanItem;
        }
        else
        {
            // Product not found
            scanItem.Status = ScanStatus.Failed;
            await _context.SaveChangesAsync(cancellationToken);
            
            return scanItem;
        }
    }
}