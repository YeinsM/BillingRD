using System.Security.Claims;
using BillingRD.Api.Security;
using BillingRD.Domain.Identity;
using BillingRD.Domain.Inventory;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Endpoints;

public static class InventoryEndpoints
{
    public static IEndpointRouteBuilder MapInventoryEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/inventory").RequireAuthorization();

        group.MapGet("/", ListAsync);
        group.MapGet("/movements", ListMovementsAsync);
        group.MapPost("/adjustments", AdjustAsync);

        return endpoints;
    }

    private static async Task<IResult> ListAsync(
        Guid branchId,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
        {
            return Results.Conflict(new { message = "Select an active business before accessing inventory." });
        }

        var branchExists = await dbContext.Branches.AnyAsync(
            branch => branch.Id == branchId && branch.IsActive,
            cancellationToken);

        if (!branchExists)
        {
            return Results.NotFound();
        }

        var balances = await dbContext.Products
            .Where(product => product.IsActive && product.TracksInventory)
            .GroupJoin(
                dbContext.StockBalances.Where(balance => balance.BranchId == branchId),
                product => product.Id,
                balance => balance.ProductId,
                (product, matches) => new
                {
                    product.Id,
                    product.Name,
                    product.Sku,
                    Quantity = matches.Select(balance => (decimal?)balance.Quantity).FirstOrDefault() ?? 0m
                })
            .OrderBy(item => item.Name)
            .ToListAsync(cancellationToken);

        return Results.Ok(balances);
    }

    private static async Task<IResult> ListMovementsAsync(
        Guid branchId,
        Guid? productId,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
        {
            return Results.Conflict(new { message = "Select an active business before accessing inventory." });
        }

        var query = dbContext.StockMovements
            .AsNoTracking()
            .Where(movement => movement.BranchId == branchId);

        if (productId.HasValue)
        {
            query = query.Where(movement => movement.ProductId == productId.Value);
        }

        var movements = await query
            .OrderByDescending(movement => movement.CreatedAtUtc)
            .Take(200)
            .Select(movement => new
            {
                movement.Id,
                movement.ProductId,
                movement.Type,
                movement.QuantityDelta,
                movement.BalanceAfter,
                movement.SaleId,
                movement.Reason,
                movement.CreatedByUserId,
                movement.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Results.Ok(movements);
    }

    private static async Task<IResult> AdjustAsync(
        AdjustmentRequest request,
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
        {
            return Results.Conflict(new { message = "Select an active business before adjusting inventory." });
        }

        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

        var role = await dbContext.BusinessMemberships
            .IgnoreQueryFilters()
            .Where(membership =>
                membership.BusinessId == currentBusiness.BusinessId.Value &&
                membership.UserId == userId)
            .Select(membership => membership.Role)
            .SingleOrDefaultAsync(cancellationToken);

        if (role is not (BusinessRole.Owner or BusinessRole.Administrator or BusinessRole.InventoryManager))
        {
            return Results.Forbid();
        }

        if (request.BranchId == Guid.Empty ||
            request.ProductId == Guid.Empty ||
            request.QuantityDelta == 0 ||
            string.IsNullOrWhiteSpace(request.Reason) ||
            request.Reason.Length > 240)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["adjustment"] = ["Branch, product, non-zero quantity delta and a reason up to 240 characters are required."]
            });
        }

        var branchExists = await dbContext.Branches.AnyAsync(
            branch => branch.Id == request.BranchId && branch.IsActive,
            cancellationToken);

        var product = await dbContext.Products.SingleOrDefaultAsync(
            candidate => candidate.Id == request.ProductId && candidate.IsActive,
            cancellationToken);

        if (!branchExists || product is null)
        {
            return Results.NotFound();
        }

        if (!product.TracksInventory)
        {
            return Results.Conflict(new { message = "This product does not track inventory." });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var exists = await dbContext.StockBalances.AnyAsync(
            balance => balance.BranchId == request.BranchId && balance.ProductId == request.ProductId,
            cancellationToken);

        decimal balanceAfter;
        bool initial;

        if (!exists)
        {
            if (request.QuantityDelta < 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Stock cannot become negative." });
            }

            var balance = StockBalance.Create(
                currentBusiness.BusinessId.Value,
                request.BranchId,
                request.ProductId,
                request.QuantityDelta);

            dbContext.StockBalances.Add(balance);
            balanceAfter = request.QuantityDelta;
            initial = true;
        }
        else
        {
            var now = DateTimeOffset.UtcNow;
            var affected = await dbContext.StockBalances
                .Where(balance =>
                    balance.BranchId == request.BranchId &&
                    balance.ProductId == request.ProductId &&
                    balance.Quantity + request.QuantityDelta >= 0)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(balance => balance.Quantity, balance => balance.Quantity + request.QuantityDelta)
                        .SetProperty(balance => balance.UpdatedAtUtc, now),
                    cancellationToken);

            if (affected != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Stock cannot become negative." });
            }

            balanceAfter = await dbContext.StockBalances
                .Where(balance => balance.BranchId == request.BranchId && balance.ProductId == request.ProductId)
                .Select(balance => balance.Quantity)
                .SingleAsync(cancellationToken);

            initial = false;
        }

        dbContext.StockMovements.Add(StockMovement.CreateAdjustment(
            currentBusiness.BusinessId.Value,
            request.BranchId,
            request.ProductId,
            userId,
            request.QuantityDelta,
            balanceAfter,
            request.Reason,
            initial));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Ok(new
        {
            request.ProductId,
            request.BranchId,
            Quantity = balanceAfter
        });
    }

    public sealed record AdjustmentRequest(
        Guid BranchId,
        Guid ProductId,
        decimal QuantityDelta,
        string Reason);
}
