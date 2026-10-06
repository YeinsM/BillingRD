using BillingRD.Domain.Billing;

namespace BillingRD.Domain.Cash;

public sealed class CashMovement
{
    private CashMovement() { }

    private CashMovement(
        Guid id,
        Guid businessId,
        Guid cashSessionId,
        Guid createdByUserId,
        CashMovementType type,
        decimal amount,
        Guid? saleId,
        Guid? paymentId,
        string? reason)
    {
        Id = id;
        BusinessId = businessId;
        CashSessionId = cashSessionId;
        CreatedByUserId = createdByUserId;
        Type = type;
        Amount = MoneyMath.RoundCurrency(amount);
        SaleId = saleId;
        PaymentId = paymentId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid CashSessionId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public CashMovementType Type { get; private set; }
    public decimal Amount { get; private set; }
    public Guid? SaleId { get; private set; }
    public Guid? PaymentId { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static CashMovement FromSale(
        Guid businessId,
        Guid cashSessionId,
        Guid userId,
        Guid saleId,
        Guid paymentId,
        decimal amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        return new CashMovement(
            Guid.CreateVersion7(),
            businessId,
            cashSessionId,
            userId,
            CashMovementType.SaleCash,
            amount,
            saleId,
            paymentId,
            null);
    }

    public static CashMovement Manual(
        Guid businessId,
        Guid cashSessionId,
        Guid userId,
        CashMovementType type,
        decimal amount,
        string reason)
    {
        if (type == CashMovementType.SaleCash) throw new ArgumentException("Sale cash movements are system generated.", nameof(type));
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var signedAmount = type switch
        {
            CashMovementType.ManualIncome => amount,
            CashMovementType.Adjustment => amount,
            CashMovementType.Expense => -amount,
            CashMovementType.Withdrawal => -amount,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported cash movement type.")
        };

        return new CashMovement(
            Guid.CreateVersion7(),
            businessId,
            cashSessionId,
            userId,
            type,
            signedAmount,
            null,
            null,
            reason);
    }
}
