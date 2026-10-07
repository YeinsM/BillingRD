namespace BillingRD.Domain.Businesses;

/// <summary>
/// Fiscal identity snapshot source for future e-CF generation.
/// This local profile does not prove DGII electronic issuer authorization.
/// </summary>
public sealed class BusinessFiscalProfile
{
    private BusinessFiscalProfile() { }

    private BusinessFiscalProfile(
        Guid id,
        Guid businessId,
        string rnc,
        string legalName,
        string? tradeName,
        string address)
    {
        Id = id;
        BusinessId = businessId;
        SetValues(rnc, legalName, tradeName, address);
        CreatedAtUtc = DateTimeOffset.UtcNow;
        UpdatedAtUtc = CreatedAtUtc;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public string Rnc { get; private set; } = string.Empty;
    public string LegalName { get; private set; } = string.Empty;
    public string? TradeName { get; private set; }
    public string Address { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static BusinessFiscalProfile Create(
        Guid businessId,
        string rnc,
        string legalName,
        string? tradeName,
        string address)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        return new BusinessFiscalProfile(Guid.CreateVersion7(), businessId, rnc, legalName, tradeName, address);
    }

    public void Update(string rnc, string legalName, string? tradeName, string address)
    {
        SetValues(rnc, legalName, tradeName, address);
        UpdatedAtUtc = DateTimeOffset.UtcNow;
    }

    private void SetValues(string rnc, string legalName, string? tradeName, string address)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rnc);
        ArgumentException.ThrowIfNullOrWhiteSpace(legalName);
        ArgumentException.ThrowIfNullOrWhiteSpace(address);

        var normalizedRnc = new string(rnc.Where(char.IsDigit).ToArray());
        if ((normalizedRnc.Length is not (9 or 11)) || !normalizedRnc.All(char.IsDigit))
            throw new ArgumentException("RNC must contain 9 or 11 digits.", nameof(rnc));

        if (legalName.Trim().Length > 150) throw new ArgumentException("Legal name cannot exceed 150 characters.", nameof(legalName));
        if (!string.IsNullOrWhiteSpace(tradeName) && tradeName.Trim().Length > 150) throw new ArgumentException("Trade name cannot exceed 150 characters.", nameof(tradeName));
        if (address.Trim().Length > 100) throw new ArgumentException("Fiscal address cannot exceed 100 characters.", nameof(address));

        Rnc = normalizedRnc;
        LegalName = legalName.Trim();
        TradeName = string.IsNullOrWhiteSpace(tradeName) ? null : tradeName.Trim();
        Address = address.Trim();
    }
}
