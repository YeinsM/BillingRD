using System.Net.Mail;
using System.Security.Claims;
using BillingRD.Api.Security;
using BillingRD.Domain.Identity;
using BillingRD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        group.MapPost("/register", RegisterAsync).RequireRateLimiting("auth");
        group.MapPost("/login", LoginAsync).RequireRateLimiting("auth");
        group.MapPost("/logout", async (HttpContext httpContext) =>
        {
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.NoContent();
        }).RequireAuthorization();
        group.MapPost("/select-business", SelectBusinessAsync).RequireAuthorization();
        group.MapGet("/me", GetMe).RequireAuthorization();

        return endpoints;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        HttpContext httpContext,
        BillingDbContext dbContext,
        IPasswordHasher<string> passwordHasher,
        CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        var validEmail = email is not null &&
            MailAddress.TryCreate(email, out var parsedEmail) &&
            string.Equals(parsedEmail.Address, email, StringComparison.OrdinalIgnoreCase);

        if (!validEmail || string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["credentials"] = ["A valid email and a password of at least 8 characters are required."]
            });
        }

        if (await dbContext.Users.AnyAsync(user => user.Email == email, cancellationToken))
        {
            return Results.Conflict(new { message = "An account with this email already exists." });
        }

        var passwordHash = passwordHasher.HashPassword(email!, request.Password);
        var user = UserAccount.Create(email!, passwordHash);

        dbContext.Users.Add(user);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { message = "An account with this email already exists." });
        }

        await AuthenticationSession.SignInAsync(httpContext, user);
        return Results.Created("/api/auth/me", new { user.Id, user.Email });
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        HttpContext httpContext,
        BillingDbContext dbContext,
        IPasswordHasher<string> passwordHasher,
        CancellationToken cancellationToken)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.Unauthorized();
        }

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Email == email && candidate.IsActive,
            cancellationToken);

        if (user is null)
        {
            return Results.Unauthorized();
        }

        var verification = passwordHasher.VerifyHashedPassword(email, user.PasswordHash, request.Password);
        if (verification == PasswordVerificationResult.Failed)
        {
            return Results.Unauthorized();
        }

        var memberships = await dbContext.BusinessMemberships
            .IgnoreQueryFilters()
            .Where(membership => membership.UserId == user.Id)
            .Join(
                dbContext.Businesses.Where(business => business.IsActive),
                membership => membership.BusinessId,
                business => business.Id,
                (membership, _) => membership.BusinessId)
            .Take(2)
            .ToListAsync(cancellationToken);

        var activeBusinessId = memberships.Count == 1 ? memberships[0] : (Guid?)null;
        await AuthenticationSession.SignInAsync(httpContext, user, activeBusinessId);

        return Results.Ok(new { user.Id, user.Email, activeBusinessId });
    }

    private static async Task<IResult> SelectBusinessAsync(
        SelectBusinessRequest request,
        HttpContext httpContext,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var membership = await dbContext.BusinessMemberships
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(
                candidate => candidate.UserId == userId && candidate.BusinessId == request.BusinessId,
                cancellationToken);

        if (membership is null)
        {
            return Results.Forbid();
        }

        var business = await dbContext.Businesses.SingleOrDefaultAsync(
            candidate => candidate.Id == request.BusinessId && candidate.IsActive,
            cancellationToken);

        var user = await dbContext.Users.SingleOrDefaultAsync(
            candidate => candidate.Id == userId && candidate.IsActive,
            cancellationToken);

        if (business is null || user is null)
        {
            return Results.Forbid();
        }

        await AuthenticationSession.SignInAsync(httpContext, user, business.Id);
        return Results.Ok(new { business.Id, business.Name, membership.Role });
    }

    private static IResult GetMe(HttpContext httpContext, CurrentBusinessContext currentBusiness)
    {
        return Results.Ok(new
        {
            userId = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier),
            email = httpContext.User.FindFirstValue(ClaimTypes.Email),
            businessId = currentBusiness.BusinessId
        });
    }

    public sealed record RegisterRequest(string? Email, string? Password);
    public sealed record LoginRequest(string? Email, string? Password);
    public sealed record SelectBusinessRequest(Guid BusinessId);
}
