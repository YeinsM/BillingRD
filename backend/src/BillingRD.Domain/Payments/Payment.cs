using BillingRD.Domain.Billing;

namespace BillingRD.Domain.Payments;

public sealed class Payment
{
    private Payment() { }

    private Payment(Guid id, Guid businessId, Guid saleId, PaymentMethod method, decimal amount, string? reference)
    {
        Id = id;
        BusinessId = businessId;
        SaleId = saleId;
        Method = method;
        Amount = MoneyMath.RoundCurrency(amount);
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid SaleId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public decimal Amount { get; private set; }
    public string? Reference { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Payment Create(Guid businessId, Guid saleId, PaymentMethod method, decimal amount, string? reference)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        if (saleId == Guid.Empty) throw new ArgumentException("Sale id is required.", nameof(saleId));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        return new Payment(Guid.CreateVersion7(), businessId, saleId, method, amount, reference);
    }
}
