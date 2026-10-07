using BillingRD.Domain.Billing;
using BillingRD.Domain.Businesses;
using BillingRD.Domain.Customers;

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
        Guid saleId,
        Guid createdByUserId,
        EcfType type,
        decimal subtotal,
        decimal taxAmount,
        decimal total)
    {
        Id = id;
        BusinessId = businessId;
        SaleId = saleId;
        CreatedByUserId = createdByUserId;
        Type = type;
        Subtotal = MoneyMath.RoundCurrency(subtotal);
        TaxAmount = MoneyMath.RoundCurrency(taxAmount);
        Total = MoneyMath.RoundCurrency(total);
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid? InvoiceId { get; private set; }
    public Guid? AdjustmentDocumentId { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid? ReturnId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public EcfType Type { get; private set; }

    public string? IssuerRnc { get; private set; }
    public string? IssuerLegalName { get; private set; }
    public string? IssuerTradeName { get; private set; }
    public string? IssuerAddress { get; private set; }

    public string? BuyerTaxId { get; private set; }
    public string? BuyerForeignIdentifier { get; private set; }
    public string? BuyerName { get; private set; }
    public string? BuyerAddress { get; private set; }

    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ElectronicFiscalDocumentDraft InvoiceFromSale(
        Guid businessId,
        Guid invoiceId,
        Guid saleId,
        Guid? customerId,
        Guid createdByUserId,
        EcfType type,
        BusinessFiscalProfile issuer,
        Customer? buyer,
        decimal subtotal,
        decimal taxAmount,
        decimal total)
    {
        if (invoiceId == Guid.Empty) throw new ArgumentException("Invoice id is required.", nameof(invoiceId));
        if (type is not (EcfType.CreditFiscalInvoice31 or EcfType.ConsumerInvoice32))
            throw new ArgumentException("Only e-CF 31 or 32 can be prepared from an internal invoice.", nameof(type));

        if (type == EcfType.CreditFiscalInvoice31)
        {
            if (buyer is null || string.IsNullOrWhiteSpace(buyer.TaxId))
                throw new InvalidOperationException("e-CF 31 requires a buyer with RNC/Cedula.");
            if (buyer.Name.Length > 150)
                throw new InvalidOperationException("Buyer name cannot exceed 150 characters for e-CF.");
        }

        if (type == EcfType.ConsumerInvoice32 && total >= 250000m)
        {
            if (buyer is null || (string.IsNullOrWhiteSpace(buyer.TaxId) && string.IsNullOrWhiteSpace(buyer.ForeignIdentifier)))
                throw new InvalidOperationException("e-CF 32 at or above DOP 250,000 requires buyer identification.");
            if (buyer.Name.Length > 150)
                throw new InvalidOperationException("Buyer name cannot exceed 150 characters for e-CF.");
        }

        var draft = new ElectronicFiscalDocumentDraft(
            Guid.CreateVersion7(),
            businessId,
            saleId,
            createdByUserId,
            type,
            subtotal,
            taxAmount,
            total)
        {
            InvoiceId = invoiceId,
            CustomerId = customerId,
            IssuerRnc = issuer.Rnc,
            IssuerLegalName = issuer.LegalName,
            IssuerTradeName = issuer.TradeName,
            IssuerAddress = issuer.Address
        };

        if (buyer is not null)
        {
            draft.BuyerTaxId = buyer.TaxId;
            draft.BuyerForeignIdentifier = buyer.ForeignIdentifier;
            draft.BuyerName = buyer.Name;
            draft.BuyerAddress = buyer.FiscalAddress;
        }

        return draft;
    }

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

        var draft = new ElectronicFiscalDocumentDraft(
            Guid.CreateVersion7(),
            businessId,
            saleId,
            createdByUserId,
            EcfType.CreditNote34,
            subtotal,
            taxAmount,
            total)
        {
            AdjustmentDocumentId = adjustmentDocumentId,
            ReturnId = returnId
        };

        return draft;
    }
}
