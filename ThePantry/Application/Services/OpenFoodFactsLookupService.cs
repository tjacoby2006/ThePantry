using System.Net.Http.Json;
using System.Text.Json;

namespace ThePantry.Application.Services;

public class OpenFoodFactsLookupService : IProductLookupService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenFoodFactsLookupService> _logger;

    public OpenFoodFactsLookupService(HttpClient httpClient, ILogger<OpenFoodFactsLookupService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ProductLookupResult?> LookupAsync(string upc, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Looking up UPC: {UPC}", upc);
            
            var response = await _httpClient.GetAsync($"{upc}.json", cancellationToken);
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UPC lookup failed for {UPC}: {StatusCode}", upc, response.StatusCode);
                return null;
            }
            
            var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            
            // Check if product was found
            if (!json.TryGetProperty("status", out var status) || status.GetInt32() != 1)
            {
                _logger.LogWarning("Product not found for UPC: {UPC}", upc);
                return null;
            }
            
            var product = json.GetProperty("product");
            
            var result = new ProductLookupResult
            {
                Success = true,
                UPC = upc
            };
            
            if (product.TryGetProperty("product_name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String)
            {
                result.Name = nameElement.GetString();
            }
            
            if (product.TryGetProperty("generic_name", out var descElement) && descElement.ValueKind == JsonValueKind.String)
            {
                result.Description = descElement.GetString();
            }
            else if (product.TryGetProperty("generic_name_en", out var descEnElement) && descEnElement.ValueKind == JsonValueKind.String)
            {
                result.Description = descEnElement.GetString();
            }
            
            if (product.TryGetProperty("brands", out var brandElement) && brandElement.ValueKind == JsonValueKind.String)
            {
                result.Brand = brandElement.GetString();
            }
            
            if (product.TryGetProperty("image_url", out var imageElement) && imageElement.ValueKind == JsonValueKind.String)
            {
                result.ImageUrl = imageElement.GetString();
            }
            
            _logger.LogInformation("Successfully looked up UPC: {UPC}, Name: {Name}", upc, result.Name);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up UPC: {UPC}", upc);
            return null;
        }
    }
}