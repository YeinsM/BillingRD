using System.Security.Claims;
using BillingRD.Api.Security;
using BillingRD.Domain.Businesses;
using BillingRD.Domain.Identity;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Endpoints;

public static class FiscalProfileEndpoints
{
    public static IEndpointRouteBuilder MapFiscalProfileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/fiscal-profile").RequireAuthorization();
        group.MapGet("/", GetAsync);
        group.MapPut("/", UpsertAsync);
        return endpoints;
    }

    private static async Task<IResult> GetAsync(
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business first." });

        var profile = await dbContext.BusinessFiscalProfiles.AsNoTracking()
            .SingleOrDefaultAsync(x => x.BusinessId == currentBusiness.BusinessId.Value, cancellationToken);

        return profile is null
            ? Results.NotFound()
            : Results.Ok(new { profile.Id, profile.Rnc, profile.LegalName, profile.TradeName, profile.Address });
    }

    private static async Task<IResult> UpsertAsync(
        UpsertFiscalProfileRequest request,
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business first." });

        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Unauthorized();

        var role = await dbContext.BusinessMemberships.IgnoreQueryFilters()
            .Where(x => x.BusinessId == currentBusiness.BusinessId.Value && x.UserId == userId)
            .Select(x => x.Role)
            .SingleOrDefaultAsync(cancellationToken);

        if (role is not (BusinessRole.Owner or BusinessRole.Administrator))
            return Results.Forbid();

        try
        {
            var profile = await dbContext.BusinessFiscalProfiles
                .SingleOrDefaultAsync(x => x.BusinessId == currentBusiness.BusinessId.Value, cancellationToken);

            if (profile is null)
            {
                profile = BusinessFiscalProfile.Create(
                    currentBusiness.BusinessId.Value,
                    request.Rnc,
                    request.LegalName,
                    request.TradeName,
                    request.Address);
                dbContext.BusinessFiscalProfiles.Add(profile);
            }
            else
            {
                profile.Update(request.Rnc, request.LegalName, request.TradeName, request.Address);
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            return Results.Ok(new { profile.Id, profile.Rnc, profile.LegalName, profile.TradeName, profile.Address });
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["fiscalProfile"] = [ex.Message] });
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { message = "The RNC is already configured for another business." });
        }
    }

    public sealed record UpsertFiscalProfileRequest(string Rnc, string LegalName, string? TradeName, string Address);
}
