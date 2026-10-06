namespace BillingRD.Domain.Cash;

public sealed class CashRegister
{
    private CashRegister() { }

    private CashRegister(Guid id, Guid businessId, Guid branchId, string name)
    {
        Id = id;
        BusinessId = businessId;
        BranchId = branchId;
        Name = name;
        IsActive = true;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid BranchId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static CashRegister Create(Guid businessId, Guid branchId, string name)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        if (branchId == Guid.Empty) throw new ArgumentException("Branch id is required.", nameof(branchId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new CashRegister(Guid.CreateVersion7(), businessId, branchId, name.Trim());
    }
}
