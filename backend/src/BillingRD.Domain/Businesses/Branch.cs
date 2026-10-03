namespace BillingRD.Domain.Businesses;

/// <summary>
/// Physical or operational location that belongs to one business.
/// </summary>
public sealed class Branch
{
    private Branch() { }

    private Branch(Guid id, Guid businessId, string name)
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
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Branch Create(Guid businessId, string name)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new Branch(Guid.CreateVersion7(), businessId, name.Trim());
    }
}
