namespace BillingRD.Domain.Billing;

/// <summary>
/// Fiscal treatment snapshot used by catalog and sales.
/// Exempt and ZeroRated are deliberately distinct even though both currently yield zero ITBIS.
/// </summary>
public enum ItbisCategory
{
    Exempt = 0,
    ZeroRated = 1,
    Reduced = 2,
    Standard = 3
}

public static class ItbisCategoryExtensions
{
    public static decimal Rate(this ItbisCategory category) => category switch
    {
        ItbisCategory.Exempt => 0m,
        ItbisCategory.ZeroRated => 0m,
        ItbisCategory.Reduced => 16m,
        ItbisCategory.Standard => 18m,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported ITBIS category.")
    };
}
