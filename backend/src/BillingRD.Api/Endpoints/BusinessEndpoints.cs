using System.Security.Claims;
using BillingRD.Api.Security;
using BillingRD.Domain.Businesses;
using BillingRD.Domain.Identity;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Endpoints;

public static class BusinessEndpoints
{
    public static IEndpointRouteBuilder MapBusinessEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/businesses").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapGet("/current", GetCurrentAsync);
        group.MapPost("/", CreateAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        HttpContext httpContext,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var businesses = await dbContext.BusinessMemberships
            .IgnoreQueryFilters()
            .Where(membership => membership.UserId == userId)
            .Join(
                dbContext.Businesses,
                membership => membership.BusinessId,
                business => business.Id,
                (membership, business) => new
                {
                    business.Id,
                    business.Name,
                    membership.Role,
                    business.IsActive
                })
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        return Results.Ok(businesses);
    }

    private static async Task<IResult> GetCurrentAsync(
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
        {
            return Results.Conflict(new { message = "No active business is selected." });
        }

        var business = await dbContext.Businesses.SingleAsync(
            item => item.Id == currentBusiness.BusinessId.Value,
            cancellationToken);

        return Results.Ok(new { business.Id, business.Name, business.LegalName, business.TaxId });
    }

    private static async Task<IResult> CreateAsync(
        CreateBusinessRequest request,
        HttpContext httpContext,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Business name is required."]
            });
        }

        var user = await dbContext.Users.SingleAsync(candidate => candidate.Id == userId, cancellationToken);
        var business = Business.Create(request.Name);
        var branch = Branch.Create(business.Id, "Principal");
        var membership = BusinessMembership.Create(business.Id, user.Id, BusinessRole.Owner);

        dbContext.Businesses.Add(business);
        dbContext.Branches.Add(branch);
        dbContext.BusinessMemberships.Add(membership);
        await dbContext.SaveChangesAsync(cancellationToken);

        await AuthenticationSession.SignInAsync(httpContext, user, business.Id);

        return Results.Created($"/api/businesses/{business.Id}", new
        {
            business.Id,
            business.Name,
            branchId = branch.Id,
            role = membership.Role
        });
    }

    public sealed record CreateBusinessRequest(string? Name);
}
