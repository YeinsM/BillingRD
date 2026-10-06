using BillingRD.Domain.Billing;
using BillingRD.Domain.Payments;

namespace BillingRD.Domain.Returns;

public sealed class SalesReturn
{
    private readonly List<ReturnLine> _lines = [];
    private readonly List<Refund> _refunds = [];

    private SalesReturn() { }

    private SalesReturn(
        Guid id,
        Guid businessId,
        Guid saleId,
        Guid branchId,
        Guid createdByUserId,
        string idempotencyKey,
        string reason)
    {
        Id = id;
        BusinessId = businessId;
        SaleId = saleId;
        BranchId = branchId;
        CreatedByUserId = createdByUserId;
        IdempotencyKey = idempotencyKey.Trim();
        Reason = reason.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid SaleId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string Reason { get; private set; } = string.Empty;
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<ReturnLine> Lines => _lines;
    public IReadOnlyCollection<Refund> Refunds => _refunds;

    public static SalesReturn Create(
        Guid businessId,
        Guid saleId,
        Guid branchId,
        Guid userId,
        string idempotencyKey,
        string reason)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        if (saleId == Guid.Empty) throw new ArgumentException("Sale id is required.", nameof(saleId));
        if (branchId == Guid.Empty) throw new ArgumentException("Branch id is required.", nameof(branchId));
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new SalesReturn(
            Guid.CreateVersion7(),
            businessId,
            saleId,
            branchId,
            userId,
            idempotencyKey,
            reason);
    }

    public void AddLine(
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
        _lines.Add(new ReturnLine(
            BusinessId,
            Id,
            SaleId,
            saleLineId,
            productId,
            productName,
            sku,
            quantity,
            unitPrice,
            itbisCategory,
            taxRate,
            subtotal,
            taxAmount,
            total));

        Recalculate();
    }

    public void AddRefund(PaymentMethod method, decimal amount, string? reference)
    {
        _refunds.Add(Refund.Create(BusinessId, Id, SaleId, method, amount, reference));
    }

    private void Recalculate()
    {
        Subtotal = MoneyMath.RoundCurrency(_lines.Sum(x => x.Subtotal));
        TaxAmount = MoneyMath.RoundCurrency(_lines.Sum(x => x.TaxAmount));
        Total = MoneyMath.RoundCurrency(_lines.Sum(x => x.Total));
    }
}
