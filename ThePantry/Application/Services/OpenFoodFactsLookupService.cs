using System.Net.Http.Json;
using System.Text.Json;

namespace ThePantry.Application.Services;

public class OpenFoodFactsLookupService : IProductLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFoodFactsLookupService> _logger;
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(800); // ~75 requests per minute (60000 / 75 = 800ms)
    
    // Simple in-memory cache
    private static readonly Dictionary<string, ProductLookupResult> _cache = new();
    private const int MaxCacheSize = 500;
    
    public OpenFoodFactsLookupService(HttpClient httpClient, ILogger<OpenFoodFactsLookupService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        var baseAddress = configuration["OpenFoodFacts:BaseAddress"] ?? "https://world.openfoodfacts.org/";
        _httpClient.BaseAddress = new Uri(baseAddress);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ThePantry/1.0 (Home Pantry Inventory App)");
    }
    
    public async Task<ProductLookupResult?> LookupAsync(string upc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(upc))
            return null;
        
        // Check cache first
        if (_cache.TryGetValue(upc, out var cached))
        {
            return cached;
        }
        
        await _rateLimiter.WaitAsync(cancellationToken);
        try
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastRequestTime;
            if (elapsed < _minInterval)
            {
                await Task.Delay(_minInterval - elapsed, cancellationToken);
            }
            
            var response = await _httpClient.GetAsync($"/api/v0/product/{upc}.json", cancellationToken);
            _lastRequestTime = DateTime.UtcNow;
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenFoodFacts API returned {StatusCode} for UPC {Upc}", response.StatusCode, upc);
                return null;
            }
            
            var json = await response.Content.ReadFromJsonAsync<OpenFoodFactsResponse>(cancellationToken: cancellationToken);
            
            if (json?.Status != 1 || json.Product == null)
            {
                _logger.LogInformation("Product not found in OpenFoodFacts for UPC {Upc}", upc);
                return null;
            }
            
            var result = new ProductLookupResult
            {
                Upc = upc,
                Name = json.Product.Product_Name ?? json.Product.Generic_Name ?? "Unknown Product",
                Description = json.Product.Generic_Name,
                Brand = json.Product.Brands,
                ImageUrl = json.Product.Image_Url ?? json.Product.Image_Front_Url
            };
            
            // Add to cache
            if (_cache.Count >= MaxCacheSize)
            {
                // Remove oldest entry (simple FIFO)
                var firstKey = _cache.Keys.First();
                _cache.Remove(firstKey);
            }
            _cache[upc] = result;
            
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up UPC {Upc} in OpenFoodFacts", upc);
            return null;
        }
        finally
        {
            _rateLimiter.Release();
        }
    }
}

// JSON response models for OpenFoodFacts API
public class OpenFoodFactsResponse
{
    public int Status { get; set; }
    public string? StatusVerbose { get; set; }
    public OpenFoodFactsProduct? Product { get; set; }
}

public class OpenFoodFactsProduct
{
    public string? Code { get; set; }
    public string? Product_Name { get; set; }
    public string? Generic_Name { get; set; }
    public string? Brands { get; set; }
    public string? Image_Url { get; set; }
    public string? Image_Front_Url { get; set; }
}