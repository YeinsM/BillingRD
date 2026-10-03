namespace BillingRD.Application.Abstractions;

/// <summary>
/// Provides the business resolved from trusted authentication/authorization context.
/// Null means no business has been securely resolved.
/// </summary>
public interface ICurrentBusinessContext
{
    Guid? BusinessId { get; }
}
