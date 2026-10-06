namespace BillingRD.Domain.Billing;

/// <summary>
/// Internal invoice snapshot. It is intentionally not an NCF/e-CF document.
/// </summary>
public sealed class Invoice
{
    private Invoice() { }

    private Invoice(Guid id, Guid businessId, Guid saleId, Guid? customerId, decimal subtotal, decimal taxAmount, decimal total)
    {
        Id = id;
        BusinessId = businessId;
        SaleId = saleId;
        CustomerId = customerId;
        Subtotal = MoneyMath.RoundCurrency(subtotal);
        TaxAmount = MoneyMath.RoundCurrency(taxAmount);
        Total = MoneyMath.RoundCurrency(total);
        IssuedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public DateTimeOffset IssuedAtUtc { get; private set; }

    public static Invoice Issue(Guid businessId, Guid saleId, Guid? customerId, decimal subtotal, decimal taxAmount, decimal total)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        if (saleId == Guid.Empty) throw new ArgumentException("Sale id is required.", nameof(saleId));

        return new Invoice(Guid.CreateVersion7(), businessId, saleId, customerId, subtotal, taxAmount, total);
    }
}
