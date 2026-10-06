namespace BillingRD.Domain.Billing;

public enum ItbisCategory
{
    Exempt = 0,
    Reduced = 16,
    Standard = 18
}

public static class ItbisCategoryExtensions
{
    public static decimal Rate(this ItbisCategory category) => category switch
    {
        ItbisCategory.Exempt => 0m,
        ItbisCategory.Reduced => 16m,
        ItbisCategory.Standard => 18m,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unsupported ITBIS category.")
    };
}
