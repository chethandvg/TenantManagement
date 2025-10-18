namespace Archu.Api.Contracts.Products;

/// <summary>
/// Represents the shape of product data exposed by the API so that clients can
/// render catalog listings, detail pages, or confirmation messages without
/// depending on the persistence model.
/// </summary>
public sealed class ProductDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public byte[] RowVersion { get; init; } = Array.Empty<byte>();
}
