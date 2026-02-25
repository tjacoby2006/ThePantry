using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Commands;
using ThePantry.Application.Services;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Services;

public class ScanProcessingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IProductLookupService _productLookupService;
    private readonly ILogger<ScanProcessingHostedService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    
    public ScanProcessingHostedService(
        IServiceProvider serviceProvider,
        IProductLookupService productLookupService,
        ILogger<ScanProcessingHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _productLookupService = productLookupService;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scan Processing Service started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingScans(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scan queue");
            }
            
            await Task.Delay(_pollingInterval, stoppingToken);
        }
        
        _logger.LogInformation("Scan Processing Service stopped");
    }
    
    private async Task ProcessPendingScans(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        // Get pending items
        var pendingItems = await context.ScanQueueItems
            .Where(s => s.Status == ScanStatus.Pending)
            .OrderBy(s => s.Timestamp)
            .Take(10) // Process in batches
            .ToListAsync(cancellationToken);
        
        foreach (var scanItem in pendingItems)
        {
            try
            {
                // Mark as processing
                scanItem.Status = ScanStatus.Processing;
                await context.SaveChangesAsync(cancellationToken);
                
                // Look up product
                var result = await _productLookupService.LookupAsync(scanItem.Upc, cancellationToken);
                
                if (result != null)
                {
                    // Product found - update scan item
                    scanItem.Status = ScanStatus.Complete;
                    scanItem.ProductName = result.Name;
                    scanItem.ProductDescription = result.Description;
                    
                    // Check if we should auto-create inventory item
                    var existingItem = await context.InventoryItems
                        .FirstOrDefaultAsync(i => i.Upc == scanItem.Upc, cancellationToken);
                    
                    if (existingItem == null)
                    {
                        // Create new inventory item
                        var newItem = new InventoryItem
                        {
                            Name = result.Name,
                            Description = result.Description,
                            Category = "Pantry",
                            OnHandCount = 1,
                            MinimumThreshold = 1,
                            Upc = scanItem.Upc,
                            CreatedDate = DateTime.UtcNow
                        };
                        
                        context.InventoryItems.Add(newItem);
                        await context.SaveChangesAsync(cancellationToken);
                        
                        scanItem.LinkedInventoryItemId = newItem.Id;
                    }
                    else
                    {
                        scanItem.LinkedInventoryItemId = existingItem.Id;
                    }
                }
                else
                {
                    // Product not found
                    scanItem.Status = ScanStatus.Failed;
                }
                
                await context.SaveChangesAsync(cancellationToken);
                _logger.LogInformation("Processed scan for UPC {Upc}: {Status}", scanItem.Upc, scanItem.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scan {ScanId} for UPC {Upc}", scanItem.Id, scanItem.Upc);
                scanItem.Status = ScanStatus.Failed;
                await context.SaveChangesAsync(cancellationToken);
            }
        }
    }
}