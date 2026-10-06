using BillingRD.Domain.Billing;

namespace BillingRD.Domain.Catalog;

/// <summary>
/// Business-owned sellable item. SalePrice is tax-exclusive in the MVP.
/// </summary>
public sealed class Product
{
    private Product() { }

    private Product(
        Guid id,
        Guid businessId,
        string name,
        string sku,
        decimal salePrice,
        ItbisCategory itbisCategory,
        bool tracksInventory)
    {
        Id = id;
        BusinessId = businessId;
        Name = name;
        Sku = sku;
        SalePrice = MoneyMath.RoundCurrency(salePrice);
        ItbisCategory = itbisCategory;
        TracksInventory = tracksInventory;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public string? Barcode { get; private set; }
    public decimal SalePrice { get; private set; }
    public ItbisCategory ItbisCategory { get; private set; } = ItbisCategory.Standard;
    public bool TracksInventory { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Product Create(
        Guid businessId,
        string name,
        string sku,
        decimal salePrice,
        ItbisCategory itbisCategory = ItbisCategory.Standard,
        bool tracksInventory = true)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(sku);
        if (salePrice < 0) throw new ArgumentOutOfRangeException(nameof(salePrice));
        _ = itbisCategory.Rate();

        return new Product(
            Guid.CreateVersion7(),
            businessId,
            name.Trim(),
            sku.Trim(),
            salePrice,
            itbisCategory,
            tracksInventory);
    }
}
