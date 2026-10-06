using BillingRD.Domain.Billing;
using BillingRD.Domain.Payments;

namespace BillingRD.Domain.Returns;

public sealed class Refund
{
    private Refund() { }

    private Refund(
        Guid id,
        Guid businessId,
        Guid returnId,
        Guid saleId,
        PaymentMethod method,
        decimal amount,
        string? reference)
    {
        Id = id;
        BusinessId = businessId;
        ReturnId = returnId;
        SaleId = saleId;
        Method = method;
        Amount = MoneyMath.RoundCurrency(amount);
        Reference = string.IsNullOrWhiteSpace(reference) ? null : reference.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid ReturnId { get; private set; }
    public Guid SaleId { get; private set; }
    public PaymentMethod Method { get; private set; }
    public decimal Amount { get; private set; }
    public string? Reference { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Refund Create(
        Guid businessId,
        Guid returnId,
        Guid saleId,
        PaymentMethod method,
        decimal amount,
        string? reference)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        return new Refund(Guid.CreateVersion7(), businessId, returnId, saleId, method, amount, reference);
    }
}
