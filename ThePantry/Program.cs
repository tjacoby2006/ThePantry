using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.EntityFrameworkCore;
using ThePantry.Application.Services;
using ThePantry.Data;
using ThePantry.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

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

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();
    context.Database.Migrate();
}

app.Run();
