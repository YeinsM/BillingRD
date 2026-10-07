namespace BillingRD.Domain.Customers;

/// <summary>
/// Customer record owned by one business.
/// Fiscal fields are optional at catalog level and validated when an e-CF type requires them.
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
    public string? ForeignIdentifier { get; private set; }
    public string? FiscalAddress { get; private set; }
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static Customer Create(
        Guid businessId,
        string name,
        string? taxId = null,
        string? foreignIdentifier = null,
        string? fiscalAddress = null,
        string? email = null,
        string? phone = null)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var customer = new Customer(Guid.CreateVersion7(), businessId, name.Trim());
        customer.UpdateFiscalIdentity(taxId, foreignIdentifier, fiscalAddress, email, phone);
        return customer;
    }

    public void UpdateFiscalIdentity(
        string? taxId,
        string? foreignIdentifier,
        string? fiscalAddress,
        string? email,
        string? phone)
    {
        var normalizedTaxId = string.IsNullOrWhiteSpace(taxId)
            ? null
            : new string(taxId.Where(char.IsDigit).ToArray());

        if (normalizedTaxId is not null && normalizedTaxId.Length is not (9 or 11))
            throw new ArgumentException("Customer RNC/Cedula must contain 9 or 11 digits.", nameof(taxId));

        if (normalizedTaxId is not null && !string.IsNullOrWhiteSpace(foreignIdentifier))
            throw new ArgumentException("Use either local tax id or foreign identifier, not both.");

        if (!string.IsNullOrWhiteSpace(foreignIdentifier) && foreignIdentifier.Trim().Length > 20)
            throw new ArgumentException("Foreign identifier cannot exceed 20 characters.", nameof(foreignIdentifier));

        if (!string.IsNullOrWhiteSpace(fiscalAddress) && fiscalAddress.Trim().Length > 100)
            throw new ArgumentException("Fiscal address cannot exceed 100 characters.", nameof(fiscalAddress));

        TaxId = normalizedTaxId;
        ForeignIdentifier = string.IsNullOrWhiteSpace(foreignIdentifier) ? null : foreignIdentifier.Trim();
        FiscalAddress = string.IsNullOrWhiteSpace(fiscalAddress) ? null : fiscalAddress.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
    }
}
