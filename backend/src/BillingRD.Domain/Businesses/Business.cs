namespace BillingRD.Domain.Businesses;

/// <summary>
/// Represents one company or independent business using BillingRD.
/// </summary>
public sealed class Business
{
    private Business() { }

    private Business(Guid id, string name)
    {
        Id = id;
        Name = name;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public string? LegalName { get; private set; }
    public string? TaxId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Business Create(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Business(Guid.CreateVersion7(), name.Trim());
    }

    public void UpdateLegalIdentity(string? legalName, string? taxId)
    {
        LegalName = string.IsNullOrWhiteSpace(legalName) ? null : legalName.Trim();
        TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim();
    }
}
