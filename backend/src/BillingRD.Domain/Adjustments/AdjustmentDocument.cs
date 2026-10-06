using BillingRD.Domain.Billing;

namespace BillingRD.Domain.Adjustments;

/// <summary>
/// Internal commercial adjustment. It is not an NCF/e-CF and has no fiscal issuance semantics.
/// </summary>
public sealed class AdjustmentDocument
{
    private AdjustmentDocument() { }

    private AdjustmentDocument(
        Guid id,
        Guid businessId,
        Guid saleId,
        Guid returnId,
        Guid createdByUserId,
        AdjustmentKind kind,
        string reason,
        decimal subtotal,
        decimal taxAmount,
        decimal total)
    {
        Id = id;
        BusinessId = businessId;
        SaleId = saleId;
        ReturnId = returnId;
        CreatedByUserId = createdByUserId;
        Kind = kind;
        Reason = reason.Trim();
        Subtotal = MoneyMath.RoundCurrency(subtotal);
        TaxAmount = MoneyMath.RoundCurrency(taxAmount);
        Total = MoneyMath.RoundCurrency(total);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid ReturnId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public AdjustmentKind Kind { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static AdjustmentDocument FromReturn(
        Guid businessId,
        Guid saleId,
        Guid returnId,
        Guid createdByUserId,
        string reason,
        decimal subtotal,
        decimal taxAmount,
        decimal total)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        if (saleId == Guid.Empty) throw new ArgumentException("Sale id is required.", nameof(saleId));
        if (returnId == Guid.Empty) throw new ArgumentException("Return id is required.", nameof(returnId));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(createdByUserId));
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        if (subtotal < 0 || taxAmount < 0 || total <= 0)
            throw new ArgumentOutOfRangeException(nameof(total));

        return new AdjustmentDocument(
            Guid.CreateVersion7(),
            businessId,
            saleId,
            returnId,
            createdByUserId,
            AdjustmentKind.Credit,
            reason,
            subtotal,
            taxAmount,
            total);
    }
}
