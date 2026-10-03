using System.Security.Claims;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Security;

/// <summary>
/// Resolves the active business only after confirming the authenticated user still owns a membership.
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
            var membershipExists = await dbContext.BusinessMemberships
                .IgnoreQueryFilters()
                .AnyAsync(
                    membership => membership.UserId == userId && membership.BusinessId == businessId,
                    httpContext.RequestAborted);

            var businessIsActive = membershipExists &&
                await dbContext.Businesses.AnyAsync(
                    business => business.Id == businessId && business.IsActive,
                    httpContext.RequestAborted);

            if (businessIsActive)
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
