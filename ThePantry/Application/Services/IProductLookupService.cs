namespace ThePantry.Application.Services;

public interface IProductLookupService
{
    Task<ProductLookupResult?> LookupAsync(string upc, CancellationToken cancellationToken = default);
}

public class ProductLookupResult
{
    public string Upc { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Brand { get; set; }
}