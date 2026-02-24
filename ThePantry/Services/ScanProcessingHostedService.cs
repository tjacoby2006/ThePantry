using MediatR;
using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Commands;
using ThePantry.Data;
using ThePantry.Domain;

namespace ThePantry.Services;

public class ScanProcessingHostedService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ScanProcessingHostedService> _logger;
    private readonly TimeSpan _pollingInterval = TimeSpan.FromSeconds(5);
    
    public ScanProcessingHostedService(
        IServiceProvider serviceProvider,
        ILogger<ScanProcessingHostedService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Scan Processing Service started");
        
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // Find pending items
                var pendingItems = await dbContext.ScanQueueItems
                    .Where(s => s.Status == ScanStatus.Pending)
                    .OrderBy(s => s.Timestamp)
                    .Take(5) // Process up to 5 at a time
                    .ToListAsync(stoppingToken);
                
                foreach (var item in pendingItems)
                {
                    try
                    {
                        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                        await mediator.Send(new ProcessScanCommand(item.Id), stoppingToken);
                        _logger.LogInformation("Processed scan for UPC: {UPC}", item.UPC);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing scan {ScanId}", item.Id);
                        item.Status = ScanStatus.Failed;
                        await dbContext.SaveChangesAsync(stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in scan processing service");
            }
            
            await Task.Delay(_pollingInterval, stoppingToken);
        }
        
        _logger.LogInformation("Scan Processing Service stopped");
    }
}