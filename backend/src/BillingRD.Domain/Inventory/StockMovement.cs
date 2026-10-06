namespace BillingRD.Domain.Inventory;

/// <summary>
/// Immutable inventory audit record. QuantityDelta is signed: positive enters stock, negative leaves stock.
/// </summary>
public sealed class StockMovement
{
    private StockMovement() { }

    private StockMovement(
        Guid id,
        Guid businessId,
        Guid branchId,
        Guid productId,
        Guid createdByUserId,
        StockMovementType type,
        decimal quantityDelta,
        decimal balanceAfter,
        Guid? saleId,
        Guid? returnId,
        string? reason)
    {
        Id = id;
        BusinessId = businessId;
        BranchId = branchId;
        ProductId = productId;
        CreatedByUserId = createdByUserId;
        Type = type;
        QuantityDelta = quantityDelta;
        BalanceAfter = balanceAfter;
        SaleId = saleId;
        ReturnId = returnId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public Guid ProductId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public StockMovementType Type { get; private set; }
    public decimal QuantityDelta { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public Guid? SaleId { get; private set; }
    public Guid? ReturnId { get; private set; }
    public string? Reason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static StockMovement CreateAdjustment(
        Guid businessId,
        Guid branchId,
        Guid productId,
        Guid createdByUserId,
        decimal quantityDelta,
        decimal balanceAfter,
        string reason,
        bool initial)
    {
        if (quantityDelta == 0) throw new ArgumentOutOfRangeException(nameof(quantityDelta));
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new StockMovement(
            Guid.CreateVersion7(),
            businessId,
            branchId,
            productId,
            createdByUserId,
            initial ? StockMovementType.Initial : StockMovementType.Adjustment,
            quantityDelta,
            balanceAfter,
            null,
            null,
            reason);
    }

    public static StockMovement CreateSale(
        Guid businessId,
        Guid branchId,
        Guid productId,
        Guid createdByUserId,
        Guid saleId,
        decimal quantity,
        decimal balanceAfter)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        return new StockMovement(
            Guid.CreateVersion7(),
            businessId,
            branchId,
            productId,
            createdByUserId,
            StockMovementType.Sale,
            -quantity,
            balanceAfter,
            saleId,
            null,
            null);
    }
    public static StockMovement CreateReturn(
        Guid businessId,
        Guid branchId,
        Guid productId,
        Guid createdByUserId,
        Guid saleId,
        Guid returnId,
        decimal quantity,
        decimal balanceAfter)
    {
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));

        return new StockMovement(
            Guid.CreateVersion7(),
            businessId,
            branchId,
            productId,
            createdByUserId,
            StockMovementType.Return,
            quantity,
            balanceAfter,
            saleId,
            returnId,
            null);
    }
}
