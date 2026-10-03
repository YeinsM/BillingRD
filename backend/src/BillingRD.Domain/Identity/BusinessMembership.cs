namespace BillingRD.Domain.Identity;

/// <summary>
/// Grants a user a role inside exactly one business.
/// </summary>
public sealed class BusinessMembership
{
    private BusinessMembership() { }

    private BusinessMembership(Guid id, Guid businessId, Guid userId, BusinessRole role)
    {
        Id = id;
        BusinessId = businessId;
        UserId = userId;
        Role = role;
        CreatedAtUtc = DateTimeOffset.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid UserId { get; private set; }
    public BusinessRole Role { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static BusinessMembership Create(Guid businessId, Guid userId, BusinessRole role)
    {
        if (businessId == Guid.Empty) throw new ArgumentException("Business id is required.", nameof(businessId));
        if (userId == Guid.Empty) throw new ArgumentException("User id is required.", nameof(userId));

        return new BusinessMembership(Guid.CreateVersion7(), businessId, userId, role);
    }
}
