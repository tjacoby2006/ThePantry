using System.Net.Http.Json;
using System.Text.Json;

namespace ThePantry.Application.Services;

public class OpenFoodFactsLookupService : IProductLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFoodFactsLookupService> _logger;
    
    // Simple in-memory cache
    private static readonly Dictionary<string, ProductLookupResult> _cache = new();
    private const int MaxCacheSize = 500;
    
    public OpenFoodFactsLookupService(HttpClient httpClient, ILogger<OpenFoodFactsLookupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri("https://world.openfoodfacts.org/");
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
        
        try
        {
            var response = await _httpClient.GetAsync($"/api/v0/product/{upc}.json", cancellationToken);
            
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
                Brand = json.Product.Brands
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
    public string? ImageUrl { get; set; }
}