using BillingRD.Domain.Billing;

namespace BillingRD.Domain.ElectronicInvoicing;

/// <summary>
/// Preparation record for future e-CF generation. It is not an issued fiscal document.
/// It intentionally contains no e-NCF, signed XML, DGII track id or acceptance status.
/// </summary>
public sealed class ElectronicFiscalDocumentDraft
{
    private ElectronicFiscalDocumentDraft() { }

    private ElectronicFiscalDocumentDraft(
        Guid id,
        Guid businessId,
        Guid adjustmentDocumentId,
        Guid saleId,
        Guid returnId,
        Guid createdByUserId,
        EcfType type,
        decimal subtotal,
        decimal taxAmount,
        decimal total)
    {
        Id = id;
        BusinessId = businessId;
        AdjustmentDocumentId = adjustmentDocumentId;
        SaleId = saleId;
        ReturnId = returnId;
        CreatedByUserId = createdByUserId;
        Type = type;
        Subtotal = MoneyMath.RoundCurrency(subtotal);
        TaxAmount = MoneyMath.RoundCurrency(taxAmount);
        Total = MoneyMath.RoundCurrency(total);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid AdjustmentDocumentId { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid ReturnId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public EcfType Type { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ElectronicFiscalDocumentDraft CreditNoteFromAdjustment(
        Guid businessId,
        Guid adjustmentDocumentId,
        Guid saleId,
        Guid returnId,
        Guid createdByUserId,
        decimal subtotal,
        decimal taxAmount,
        decimal total)
    {
        if (adjustmentDocumentId == Guid.Empty)
            throw new ArgumentException("Adjustment document id is required.", nameof(adjustmentDocumentId));

        return new ElectronicFiscalDocumentDraft(
            Guid.CreateVersion7(),
            businessId,
            adjustmentDocumentId,
            saleId,
            returnId,
            createdByUserId,
            EcfType.CreditNote34,
            subtotal,
            taxAmount,
            total);
    }
}
