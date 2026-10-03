namespace BillingRD.Domain.Customers;

/// <summary>
/// Customer record owned by one business.
/// </summary>
public sealed class Customer
{
    private Customer() { }

    private Customer(Guid id, Guid businessId, string name)
    {
        Id = id;
        BusinessId = businessId;
        Name = name;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? TaxId { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Customer Create(Guid businessId, string name)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Customer(Guid.CreateVersion7(), businessId, name.Trim());
    }
}
