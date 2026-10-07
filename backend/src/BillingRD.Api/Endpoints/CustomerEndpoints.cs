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
        group.MapPut("/{customerId:guid}/fiscal-identity", UpdateFiscalIdentityAsync);

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
                customer.ForeignIdentifier,
                customer.FiscalAddress,
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

        Customer customer;
        try
        {
            customer = Customer.Create(
                currentBusiness.BusinessId.Value,
                request.Name,
                request.TaxId,
                request.ForeignIdentifier,
                request.FiscalAddress,
                request.Email,
                request.Phone);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["customer"] = [ex.Message] });
        }

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/customers/{customer.Id}", new
        {
            customer.Id,
            customer.Name,
            customer.TaxId,
            customer.ForeignIdentifier,
            customer.FiscalAddress,
            customer.Email,
            customer.Phone
        });
    }

    private static async Task<IResult> UpdateFiscalIdentityAsync(
        Guid customerId,
        FiscalIdentityRequest request,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business first." });

        var customer = await dbContext.Customers.SingleOrDefaultAsync(x => x.Id == customerId && x.IsActive, cancellationToken);
        if (customer is null) return Results.NotFound();

        try
        {
            customer.UpdateFiscalIdentity(request.TaxId, request.ForeignIdentifier, request.FiscalAddress, request.Email, request.Phone);
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["customer"] = [ex.Message] });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return Results.Ok(new { customer.Id, customer.Name, customer.TaxId, customer.ForeignIdentifier, customer.FiscalAddress, customer.Email, customer.Phone });
    }

    public sealed record CreateCustomerRequest(
        string? Name,
        string? TaxId = null,
        string? ForeignIdentifier = null,
        string? FiscalAddress = null,
        string? Email = null,
        string? Phone = null);

    public sealed record FiscalIdentityRequest(
        string? TaxId,
        string? ForeignIdentifier,
        string? FiscalAddress,
        string? Email,
        string? Phone);
}
