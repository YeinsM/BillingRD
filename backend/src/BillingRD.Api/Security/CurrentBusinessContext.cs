using BillingRD.Application.Abstractions;

namespace BillingRD.Api.Security;

/// <summary>
/// Request-scoped business context. Only trusted middleware may resolve it from an authenticated membership.
/// </summary>
public sealed class CurrentBusinessContext : ICurrentBusinessContext
{
    public Guid? BusinessId { get; private set; }

    internal void Resolve(Guid businessId)
    {
        if (businessId == Guid.Empty)
        {
            throw new ArgumentException("Business id cannot be empty.", nameof(businessId));
        }

        BusinessId = businessId;
    }
}
