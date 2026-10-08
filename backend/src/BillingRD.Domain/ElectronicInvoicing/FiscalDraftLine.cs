using BillingRD.Domain.Billing;
using BillingRD.Domain.Catalog;
using BillingRD.Domain.Sales;

namespace BillingRD.Domain.ElectronicInvoicing;

public sealed class FiscalDraftLine
{
    private FiscalDraftLine() { }

    private FiscalDraftLine(Guid businessId, Guid draftId, int number, SaleLine line)
    {
        if (line.ProductKind == ProductKind.Unknown)
            throw new InvalidOperationException($"Product '{line.ProductName}' must be classified as Good or Service before preparing an e-CF.");
        if (decimal.Round(line.Quantity, 2) != line.Quantity)
            throw new InvalidOperationException($"Product '{line.ProductName}' quantity cannot exceed 2 decimal places for e-CF.");

        Id = Guid.CreateVersion7();
        BusinessId = businessId;
        DraftId = draftId;
        Number = number;
        ProductId = line.ProductId;
        Sku = line.Sku.Length <= 35 ? line.Sku : line.Sku[..35];
        Name = line.ProductName.Length <= 80 ? line.ProductName : line.ProductName[..80];
        ProductKind = line.ProductKind;
        BillingIndicator = line.ItbisCategory switch
        {
            ItbisCategory.Standard => "1",
            ItbisCategory.Reduced => "2",
            ItbisCategory.ZeroRated => "3",
            ItbisCategory.Exempt => "E",
            _ => throw new ArgumentOutOfRangeException(nameof(line.ItbisCategory))
        };
        Quantity = line.Quantity;
        UnitPrice = line.UnitPrice;
        Amount = line.Subtotal;
        TaxAmount = line.TaxAmount;
        TaxRate = line.TaxRate;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid DraftId { get; private set; }
    public int Number { get; private set; }
    public Guid ProductId { get; private set; }
    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public ProductKind ProductKind { get; private set; }
    public string BillingIndicator { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public decimal Amount { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal TaxRate { get; private set; }

    internal static FiscalDraftLine FromSaleLine(Guid businessId, Guid draftId, int number, SaleLine line) =>
        new(businessId, draftId, number, line);
}
