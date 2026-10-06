using BillingRD.Domain.Billing;

namespace BillingRD.Domain.Cash;

public sealed class CashSession
{
    private CashSession() { }

    private CashSession(Guid id, Guid businessId, Guid cashRegisterId, Guid openedByUserId, decimal openingBalance)
    {
        Id = id;
        BusinessId = businessId;
        CashRegisterId = cashRegisterId;
        OpenedByUserId = openedByUserId;
        OpeningBalance = MoneyMath.RoundCurrency(openingBalance);
        OpenedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid CashRegisterId { get; private set; }
    public Guid OpenedByUserId { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public DateTimeOffset OpenedAtUtc { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public decimal? ExpectedCashAtClose { get; private set; }
    public decimal? CountedCash { get; private set; }
    public decimal? Difference { get; private set; }

    public bool IsOpen => ClosedAtUtc is null;

    public static CashSession Open(Guid businessId, Guid cashRegisterId, Guid userId, decimal openingBalance)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        if (cashRegisterId == Guid.Empty) throw new ArgumentException("Cash register id is required.", nameof(cashRegisterId));
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        if (openingBalance < 0) throw new ArgumentOutOfRangeException(nameof(openingBalance));

        return new CashSession(Guid.CreateVersion7(), businessId, cashRegisterId, userId, openingBalance);
    }

    public void Close(Guid userId, decimal expectedCash, decimal countedCash)
    {
        if (!IsOpen) throw new InvalidOperationException("Cash session is already closed.");
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        if (countedCash < 0) throw new ArgumentOutOfRangeException(nameof(countedCash));

        var expected = MoneyMath.RoundCurrency(expectedCash);
        var counted = MoneyMath.RoundCurrency(countedCash);

        ClosedByUserId = userId;
        ClosedAtUtc = DateTimeOffset.UtcNow;
        ExpectedCashAtClose = expected;
        CountedCash = counted;
        Difference = MoneyMath.RoundCurrency(counted - expected);
    }
}
