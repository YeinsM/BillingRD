namespace BillingRD.Domain.Identity;

/// <summary>
/// Application identity record. Credentials are intentionally not modeled in the domain.
/// </summary>
public sealed class UserAccount
{
    private UserAccount() { }

    private UserAccount(Guid id, string email)
    {
        Id = id;
        Email = email;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static UserAccount Create(string email)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        return new UserAccount(Guid.CreateVersion7(), email.Trim().ToLowerInvariant());
    }
}
