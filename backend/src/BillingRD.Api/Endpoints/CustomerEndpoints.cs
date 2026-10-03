using BillingRD.Api.Security;
using BillingRD.Domain.Customers;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Endpoints;

public static class CustomerEndpoints
{
    public static IEndpointRouteBuilder MapCustomerEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/customers").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapPost("/", CreateAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
        {
            return Results.Conflict(new { message = "Select an active business before accessing customers." });
        }

        var customers = await dbContext.Customers
            .OrderBy(customer => customer.Name)
            .Select(customer => new
            {
                customer.Id,
                customer.Name,
                customer.TaxId,
                customer.Email,
                customer.Phone,
                customer.IsActive
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(customers);
    }

    private static async Task<IResult> CreateAsync(
        CreateCustomerRequest request,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
        {
            return Results.Conflict(new { message = "Select an active business before creating customers." });
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = ["Customer name is required."]
            });
        }

        var customer = Customer.Create(currentBusiness.BusinessId.Value, request.Name);
        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/customers/{customer.Id}", new
        {
            customer.Id,
            customer.Name
        });
    }

    public sealed record CreateCustomerRequest(string? Name);
}
