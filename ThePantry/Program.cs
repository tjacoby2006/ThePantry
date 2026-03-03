using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Services;
using ThePantry.Data;
using ThePantry.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor()
    .AddHubOptions(options =>
    {
        options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
    });

// Database - SQLite
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection") ?? "Data Source=thepantry.db"));

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// HTTP Client for OpenFoodFacts API
builder.Services.AddHttpClient<IProductLookupService, OpenFoodFactsLookupService>();

// Toast Service
builder.Services.AddScoped<ToastService>();

// Background service for scan processing
builder.Services.AddHostedService<ScanProcessingHostedService>();

var app = builder.Build();

// Ensure scan storage directory exists
var scanStoragePath = app.Configuration["ScanStoragePath"] ?? "wwwroot/uploads/scans";
if (!Directory.Exists(scanStoragePath))
{
    Directory.CreateDirectory(scanStoragePath);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

// API for database backup
app.MapGet("/api/backup", async (ApplicationDbContext context) =>
{
    var dbPath = context.Database.GetDbConnection().DataSource;
    if (File.Exists(dbPath))
    {
        var bytes = await File.ReadAllBytesAsync(dbPath);
        return Results.File(bytes, "application/x-sqlite3", $"thepantry_backup_{DateTime.Now:yyyyMMdd}.db");
    }
    return Results.NotFound();
});

app.MapGet("/uploads/scans/{filename}", async (string filename) =>
{
    var filePath = Path.Combine(scanStoragePath, filename);
    if (File.Exists(filePath))
    {
        return Results.File(filePath);
    }
    return Results.NotFound();
});

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    
    // Check for pending migrations
    var pendingMigrations = context.Database.GetPendingMigrations().ToList();
    if (pendingMigrations.Any())
    {
        Console.WriteLine($"Found {pendingMigrations.Count} pending migrations. Backing up database before applying...");
        
        var dbPath = context.Database.GetDbConnection().DataSource;
        if (File.Exists(dbPath))
        {
            var dbDirectory = Path.GetDirectoryName(Path.GetFullPath(dbPath)) ?? app.Environment.ContentRootPath;
            var backupDir = Path.Combine(dbDirectory, "backups");
            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }
            
            var backupPath = Path.Combine(backupDir, $"pre_migration_{DateTime.Now:yyyyMMdd_HHmmss}.db");
            File.Copy(dbPath, backupPath);
            Console.WriteLine($"Backup created at: {backupPath}");
        }
        
        context.Database.Migrate();
        Console.WriteLine("Migrations applied successfully.");
    }
    else
    {
        context.Database.Migrate();
    }
}

app.Run();
