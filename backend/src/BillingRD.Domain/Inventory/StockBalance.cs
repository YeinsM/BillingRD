namespace BillingRD.Domain.Inventory;

/// <summary>
/// Current materialized stock for one product at one branch.
/// Movements remain the audit trail; this balance is optimized for operational reads/concurrency checks.
/// </summary>
public sealed class StockBalance
{
    private StockBalance() { }

    private StockBalance(Guid id, Guid businessId, Guid branchId, Guid productId, decimal quantity)
    {
        Id = id;
        BusinessId = businessId;
        BranchId = branchId;
        ProductId = productId;
        Quantity = quantity;
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid ProductId { get; private set; }
    public decimal Quantity { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static StockBalance Create(Guid businessId, Guid branchId, Guid productId, decimal quantity = 0m)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        if (branchId == Guid.Empty) throw new ArgumentException("Branch id is required.", nameof(branchId));
        if (productId == Guid.Empty) throw new ArgumentException("Product id is required.", nameof(productId));
        if (quantity < 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        return new StockBalance(Guid.CreateVersion7(), businessId, branchId, productId, quantity);
    }
}
