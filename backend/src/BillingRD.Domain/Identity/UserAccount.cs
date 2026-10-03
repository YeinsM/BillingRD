namespace BillingRD.Domain.Identity;

/// <summary>
/// Application identity record. Password hashing is performed outside the domain using ASP.NET Core's password hasher.
/// </summary>
public sealed class UserAccount
{
    private UserAccount() { }

    private UserAccount(Guid id, string email, string passwordHash)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        CreatedAtUtc = DateTimeOffset.UtcNow;
        IsActive = true;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static UserAccount Create(string email, string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        return new UserAccount(
            Guid.CreateVersion7(),
            email.Trim().ToLowerInvariant(),
            passwordHash);
    }
}
