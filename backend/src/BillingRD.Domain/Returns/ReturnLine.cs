using BillingRD.Domain.Billing;

namespace BillingRD.Domain.Returns;

public sealed class ReturnLine
{
    private ReturnLine() { }

    internal ReturnLine(
        Guid businessId,
        Guid returnId,
        Guid saleId,
        Guid saleLineId,
        Guid productId,
        string productName,
        string sku,
        decimal quantity,
        decimal unitPrice,
        ItbisCategory itbisCategory,
        decimal taxRate,
        decimal subtotal,
        decimal taxAmount,
        decimal total)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        Id = Guid.CreateVersion7();
        BusinessId = businessId;
        ReturnId = returnId;
        SaleId = saleId;
        SaleLineId = saleLineId;
        ProductId = productId;
        ProductName = productName;
        Sku = sku;
        Quantity = quantity;
        UnitPrice = unitPrice;
        ItbisCategory = itbisCategory;
        TaxRate = taxRate;
        Subtotal = MoneyMath.RoundCurrency(subtotal);
        TaxAmount = MoneyMath.RoundCurrency(taxAmount);
        Total = MoneyMath.RoundCurrency(total);
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid ReturnId { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid SaleLineId { get; private set; }
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
