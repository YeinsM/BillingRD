using System.Security.Claims;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Security;

/// <summary>
/// Resolves the active business only after confirming the authenticated user, business and membership are still valid.
/// </summary>
public sealed class BusinessContextMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext)
    {
        if (httpContext.User.Identity?.IsAuthenticated == true &&
            Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) &&
            Guid.TryParse(httpContext.User.FindFirstValue(AuthClaims.BusinessId), out var businessId))
        {
            var canAccessBusiness = await dbContext.BusinessMemberships
                .IgnoreQueryFilters()
                .Where(membership => membership.UserId == userId && membership.BusinessId == businessId)
                .Join(
                    dbContext.Users.Where(user => user.IsActive),
                    membership => membership.UserId,
                    user => user.Id,
                    (membership, _) => membership)
                .Join(
                    dbContext.Businesses.Where(business => business.IsActive),
                    membership => membership.BusinessId,
                    business => business.Id,
                    (membership, _) => membership)
                .AnyAsync(httpContext.RequestAborted);

            if (canAccessBusiness)
            {
                currentBusiness.Resolve(businessId);
            }
        }

        await next(httpContext);
    }
}

public static class AuthClaims
{
    public const string BusinessId = "billingrd:business_id";
}
