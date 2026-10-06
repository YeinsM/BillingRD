using BillingRD.Domain.Billing;

namespace BillingRD.Domain.Sales;

public sealed class Sale
{
    private readonly List<SaleLine> _lines = [];

    private Sale() { }

    private Sale(Guid id, Guid businessId, Guid branchId, Guid? customerId, Guid createdByUserId)
    {
        Id = id;
        BusinessId = businessId;
        BranchId = branchId;
        CustomerId = customerId;
        CreatedByUserId = createdByUserId;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal TaxAmount { get; private set; }
    public decimal Total { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<SaleLine> Lines => _lines;

    public static Sale Create(Guid businessId, Guid branchId, Guid? customerId, Guid createdByUserId)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        if (branchId == Guid.Empty) throw new ArgumentException("Branch id is required.", nameof(branchId));
        if (createdByUserId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(createdByUserId));

        return new Sale(Guid.CreateVersion7(), businessId, branchId, customerId, createdByUserId);
    }

    public void AddLine(
        Guid productId,
        string productName,
        string sku,
        decimal quantity,
        decimal unitPrice,
        ItbisCategory itbisCategory)
    {
        var line = new SaleLine(Id, productId, productName, sku, quantity, unitPrice, itbisCategory);
        _lines.Add(line);
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        Subtotal = MoneyMath.RoundCurrency(_lines.Sum(line => line.Subtotal));
        TaxAmount = MoneyMath.RoundCurrency(_lines.Sum(line => line.TaxAmount));
        Total = MoneyMath.RoundCurrency(Subtotal + TaxAmount);
    }
}
