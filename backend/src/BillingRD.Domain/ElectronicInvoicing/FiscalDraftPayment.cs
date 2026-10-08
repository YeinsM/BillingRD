using BillingRD.Domain.Payments;

namespace BillingRD.Domain.ElectronicInvoicing;

public sealed class FiscalDraftPayment
{
    private FiscalDraftPayment() { }

    private FiscalDraftPayment(Guid businessId, Guid draftId, Payment payment)
    {
        Id = Guid.CreateVersion7();
        BusinessId = businessId;
        DraftId = draftId;
        SourcePaymentId = payment.Id;
        FormCode = payment.Method switch
        {
            PaymentMethod.Cash => 1,
            PaymentMethod.BankTransfer => 2,
            PaymentMethod.Card => 3,
            _ => 8
        };
        Amount = payment.Amount;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid DraftId { get; private set; }
    public Guid SourcePaymentId { get; private set; }
    public int FormCode { get; private set; }
    public decimal Amount { get; private set; }

    internal static FiscalDraftPayment FromPayment(Guid businessId, Guid draftId, Payment payment) =>
        new(businessId, draftId, payment);
}
