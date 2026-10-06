using BillingRD.Domain.Billing;

namespace BillingRD.Domain.Sales;

public sealed class SaleLine
{
    private SaleLine() { }

    internal SaleLine(
        Guid businessId,
        Guid saleId,
        Guid productId,
        string productName,
        string sku,
        decimal quantity,
        decimal unitPrice,
        ItbisCategory itbisCategory)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        Id = Guid.CreateVersion7();
        BusinessId = businessId;
        SaleId = saleId;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        Quantity = quantity;
        UnitPrice = MoneyMath.RoundCurrency(unitPrice);
        ItbisCategory = itbisCategory;
        TaxRate = itbisCategory.Rate();

        Subtotal = MoneyMath.RoundCurrency(Quantity * UnitPrice);
        TaxAmount = MoneyMath.RoundCurrency(Subtotal * TaxRate / 100m);
        Total = MoneyMath.RoundCurrency(Subtotal + TaxAmount);
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public string Sku { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public ItbisCategory ItbisCategory { get; private set; }
    public decimal TaxRate { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
}
