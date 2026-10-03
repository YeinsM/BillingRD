using BillingRD.Api.Security;
using BillingRD.Domain.Catalog;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/products").RequireAuthorization();

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
            return Results.Conflict(new { message = "Select an active business before accessing products." });
        }

        var products = await dbContext.Products
            .OrderBy(product => product.Name)
            .Select(product => new
            {
                product.Id,
                product.Name,
                product.Sku,
                product.Barcode,
                product.SalePrice,
                product.IsActive
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(products);
    }

    private static async Task<IResult> CreateAsync(
        CreateProductRequest request,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
        {
            return Results.Conflict(new { message = "Select an active business before creating products." });
        }

        if (string.IsNullOrWhiteSpace(request.Name) ||
            string.IsNullOrWhiteSpace(request.Sku) ||
            request.SalePrice < 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["product"] = ["Name, SKU and a non-negative sale price are required."]
            });
        }

        var product = Product.Create(
            currentBusiness.BusinessId.Value,
            request.Name,
            request.Sku,
            request.SalePrice);

        dbContext.Products.Add(product);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { message = "The SKU already exists in the active business." });
        }

        return Results.Created($"/api/products/{product.Id}", new
        {
            product.Id,
            product.Name,
            product.Sku,
            product.SalePrice
        });
    }

    public sealed record CreateProductRequest(string? Name, string? Sku, decimal SalePrice);
}
