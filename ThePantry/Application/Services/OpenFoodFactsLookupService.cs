using System.Net.Http.Json;
using System.Text.Json;

namespace ThePantry.Application.Services;

public class OpenFoodFactsLookupService : IProductLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFoodFactsLookupService> _logger;
    private readonly string _cachePath;
    private static readonly SemaphoreSlim _rateLimiter = new(1, 1);
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private static readonly TimeSpan _minInterval = TimeSpan.FromMilliseconds(60000/50); // 50 requests per minute
    
    // Simple in-memory cache for performance, but we'll also use file-based cache
    private static readonly Dictionary<string, ProductLookupResult> _memoryCache = new();
    private const int MaxMemoryCacheSize = 500;
    
    public OpenFoodFactsLookupService(HttpClient httpClient, ILogger<OpenFoodFactsLookupService> logger, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        var baseAddress = configuration["OpenFoodFacts:BaseAddress"] ?? "https://world.openfoodfacts.org/";
        _httpClient.BaseAddress = new Uri(baseAddress);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "ThePantry/1.0 (Home Pantry Inventory App)");
        
        _cachePath = configuration["ProductCachePath"] ?? "uploads/product_cache";
        if (!Path.IsPathRooted(_cachePath))
        {
            _cachePath = Path.Combine(AppContext.BaseDirectory, _cachePath);
        }
        
        if (!Directory.Exists(_cachePath))
        {
            Directory.CreateDirectory(_cachePath);
        }
    }
    
    public async Task<ProductLookupResult?> LookupAsync(string upc, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(upc))
            return null;
        
        // 1. Check memory cache first
        if (_memoryCache.TryGetValue(upc, out var cached))
        {
            return cached;
        }

        // 2. Check file cache
        var cacheFilePath = Path.Combine(_cachePath, $"{upc}.json");
        if (File.Exists(cacheFilePath))
        {
            try
            {
                var cachedJson = await File.ReadAllTextAsync(cacheFilePath, cancellationToken);
                var apiResponse = JsonSerializer.Deserialize<OpenFoodFactsResponse>(cachedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (apiResponse?.Status == 1 && apiResponse.Product != null)
                {
                    var result = MapToResult(upc, apiResponse.Product);
                    AddToMemoryCache(upc, result);
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading cached product for UPC {Upc}", upc);
            }
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
            
            var rawJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var apiResponse = JsonSerializer.Deserialize<OpenFoodFactsResponse>(rawJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            
            if (apiResponse?.Status != 1 || apiResponse.Product == null)
            {
                _logger.LogInformation("Product not found in OpenFoodFacts for UPC {Upc}", upc);
                return null;
            }
            
            var result = MapToResult(upc, apiResponse.Product);
            
            // 3. Save raw JSON to file cache
            try
            {
                await File.WriteAllTextAsync(cacheFilePath, rawJson, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving product to cache for UPC {Upc}", upc);
            }

            // 4. Add to memory cache
            AddToMemoryCache(upc, result);
            
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

    private static ProductLookupResult MapToResult(string upc, OpenFoodFactsProduct product)
    {
        return new ProductLookupResult
        {
            Upc = upc,
            Name = product.Product_Name ?? product.Generic_Name ?? "Unknown Product",
            Description = product.Generic_Name,
            Brand = product.Brands,
            ImageUrl = product.Image_Url ?? product.Image_Front_Url
        };
    }

    private void AddToMemoryCache(string upc, ProductLookupResult result)
    {
        if (_memoryCache.Count >= MaxMemoryCacheSize)
        {
            var firstKey = _memoryCache.Keys.First();
            _memoryCache.Remove(firstKey);
        }
        _memoryCache[upc] = result;
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