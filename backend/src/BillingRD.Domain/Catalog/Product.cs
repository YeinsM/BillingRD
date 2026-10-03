namespace BillingRD.Domain.Catalog;

/// <summary>
/// Business-owned sellable item. Tax behavior and inventory policy are intentionally deferred.
/// </summary>
public sealed class Product
{
    private Product() { }

    private Product(Guid id, Guid businessId, string name, string sku, decimal salePrice)
    {
        Id = id;
        BusinessId = businessId;
        Name = name;
        Sku = sku;
        SalePrice = salePrice;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public decimal SalePrice { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Product Create(Guid businessId, string name, string sku, decimal salePrice)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        if (salePrice < 0) throw new ArgumentOutOfRangeException(nameof(salePrice));

        return new Product(Guid.CreateVersion7(), businessId, name.Trim(), sku.Trim(), salePrice);
    }
}
