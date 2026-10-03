namespace BillingRD.Domain.Identity;

/// <summary>
/// Initial business-level authorization roles. Authentication provider remains a separate decision.
/// </summary>
public enum BusinessRole
{
    Owner = 1,
    Administrator = 2,
    Cashier = 3,
    InventoryManager = 4
}
