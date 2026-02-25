using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Application.Commands;

public record QueueScanCommand(
    string Upc,
    string? RawData = null,
    string? ImageData = null
) : IRequest<ScanQueueItem>;

public class QueueScanHandler : IRequestHandler<QueueScanCommand, ScanQueueItem>
{
    private readonly ApplicationDbContext _context;
    private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _environment;
    
    public QueueScanHandler(ApplicationDbContext context, Microsoft.AspNetCore.Hosting.IWebHostEnvironment environment)
    {
        _context = context;
        _environment = environment;
    }
    
    public async Task<ScanQueueItem> Handle(QueueScanCommand request, CancellationToken cancellationToken)
    {
        // Check for recent duplicate scans (within last 5 seconds) to prevent double-queuing
        var recentScan = await _context.ScanQueueItems
            .Where(s => s.Upc == request.Upc && s.Timestamp > DateTime.UtcNow.AddSeconds(-5))
            .FirstOrDefaultAsync(cancellationToken);

        if (recentScan != null)
        {
            return recentScan;
        }

        string? imagePath = null;
        if (!string.IsNullOrEmpty(request.ImageData))
        {
            try
            {
                var base64Data = request.ImageData;
                if (base64Data.Contains(","))
                {
                    base64Data = base64Data.Split(',')[1];
                }
                
                var bytes = Convert.FromBase64String(base64Data);
                var fileName = $"scan_{Guid.NewGuid()}.jpg";
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "scans");
                
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }
                
                var filePath = Path.Combine(uploadsFolder, fileName);
                await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
                imagePath = $"/uploads/scans/{fileName}";
            }
            catch (Exception)
            {
                // Log error or handle silently
            }
        }

        // Check if we already have this UPC in inventory
        var existingItem = await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.Skus.Any(s => s.Sku == request.Upc), cancellationToken);
        
        var scanItem = new ScanQueueItem
        {
            Upc = request.Upc,
            RawData = request.RawData,
            Status = ScanStatus.Pending,
            Timestamp = DateTime.UtcNow,
            LinkedInventoryItemId = existingItem?.Id,
            ImagePath = imagePath
        };
        
        _context.ScanQueueItems.Add(scanItem);
        await _context.SaveChangesAsync(cancellationToken);
        
        return scanItem;
    }
}