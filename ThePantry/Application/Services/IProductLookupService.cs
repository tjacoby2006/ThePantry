namespace ThePantry.Application.Services;

public class ProductLookupResult
{
    public bool Success { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? UPC { get; set; }
    public string? Brand { get; set; }
    public string? ImageUrl { get; set; }
}

public interface IProductLookupService
{
    Task<ProductLookupResult?> LookupAsync(string upc, CancellationToken cancellationToken = default);
}