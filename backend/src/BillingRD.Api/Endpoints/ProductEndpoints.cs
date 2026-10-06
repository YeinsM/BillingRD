using BillingRD.Api.Security;
using BillingRD.Domain.Billing;
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
                product.ItbisCategory,
                product.TracksInventory,
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
            request.SalePrice < 0 ||
            !Enum.IsDefined(request.ItbisCategory))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["product"] = ["Name, SKU, non-negative sale price and a supported ITBIS category are required."]
            });
        }

        var normalizedSku = request.Sku.Trim();

        var skuExists = await dbContext.Products.AnyAsync(
            product => product.Sku == normalizedSku,
            cancellationToken);

        if (skuExists)
        {
            return Results.Conflict(new { message = "The SKU already exists in the active business." });
        }

        var product = Product.Create(
            currentBusiness.BusinessId.Value,
            request.Name,
            normalizedSku,
            request.SalePrice,
            request.ItbisCategory,
            request.TracksInventory);

        dbContext.Products.Add(product);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Results.Created($"/api/products/{product.Id}", new
        {
            product.Id,
            product.Name,
            product.Sku,
            product.SalePrice,
            product.ItbisCategory,
            product.TracksInventory
        });
    }

    public sealed record CreateProductRequest(
        string? Name,
        string? Sku,
        decimal SalePrice,
        ItbisCategory ItbisCategory = ItbisCategory.Standard,
        bool TracksInventory = true);
}
