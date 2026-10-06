using System.Security.Claims;
using BillingRD.Api.Security;
using BillingRD.Domain.Billing;
using BillingRD.Domain.Cash;
using BillingRD.Domain.Identity;
using BillingRD.Domain.Inventory;
using BillingRD.Domain.Payments;
using BillingRD.Domain.Returns;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Endpoints;

public static class ReturnEndpoints
{
    public static IEndpointRouteBuilder MapReturnEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/returns").RequireAuthorization();
        group.MapPost("/", CreateAsync);
        group.MapGet("/{returnId:guid}", GetAsync);
        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateReturnRequest request,
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before creating a return." });

        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Unauthorized();

        if (!httpContext.Request.Headers.TryGetValue("Idempotency-Key", out var idempotencyHeader) ||
            string.IsNullOrWhiteSpace(idempotencyHeader) ||
            idempotencyHeader.ToString().Length > 100)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Idempotency-Key"] = ["A non-empty Idempotency-Key header with at most 100 characters is required."]
            });
        }

        var idempotencyKey = idempotencyHeader.ToString().Trim();

        var existing = await dbContext.Returns
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existing is not null)
            return await BuildResultAsync(existing.Id, dbContext, cancellationToken);

        var role = await dbContext.BusinessMemberships
            .IgnoreQueryFilters()
            .Where(x => x.BusinessId == currentBusiness.BusinessId.Value && x.UserId == userId)
            .Select(x => x.Role)
            .SingleOrDefaultAsync(cancellationToken);

        if (role is not (BusinessRole.Owner or BusinessRole.Administrator or BusinessRole.Cashier))
            return Results.Forbid();

        if (request.SaleId == Guid.Empty ||
            string.IsNullOrWhiteSpace(request.Reason) ||
            request.Reason.Length > 240 ||
            request.Items is null || request.Items.Count == 0 ||
            request.Refunds is null || request.Refunds.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["return"] = ["Sale, reason, at least one returned item and at least one refund are required."]
            });
        }

        if (request.Items.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0) ||
            request.Items.Select(x => x.ProductId).Distinct().Count() != request.Items.Count)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["Each product must appear once and returned quantities must be greater than zero."]
            });
        }

        if (request.Refunds.Any(x => !Enum.IsDefined(x.Method) || x.Amount <= 0))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["refunds"] = ["Every refund must use a supported method and a positive amount."]
            });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var sale = await dbContext.Sales
            .FromSqlInterpolated($@"SELECT * FROM sales WHERE ""Id"" = {request.SaleId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (sale is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.NotFound();
        }

        var saleLines = await dbContext.SaleLines
            .Where(x => x.SaleId == sale.Id)
            .ToListAsync(cancellationToken);

        var saleLinesByProduct = saleLines.ToDictionary(x => x.ProductId);

        if (request.Items.Any(x => !saleLinesByProduct.ContainsKey(x.ProductId)))
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["One or more products were not part of the original sale."]
            });
        }

        var previousLines = await dbContext.ReturnLines
            .Where(x => x.SaleId == sale.Id)
            .GroupBy(x => x.SaleLineId)
            .Select(group => new
            {
                SaleLineId = group.Key,
                Quantity = group.Sum(x => x.Quantity),
                Subtotal = group.Sum(x => x.Subtotal),
                TaxAmount = group.Sum(x => x.TaxAmount),
                Total = group.Sum(x => x.Total)
            })
            .ToDictionaryAsync(x => x.SaleLineId, cancellationToken);

        var salesReturn = SalesReturn.Create(
            currentBusiness.BusinessId.Value,
            sale.Id,
            sale.BranchId,
            userId,
            idempotencyKey,
            request.Reason);

        foreach (var item in request.Items)
        {
            var original = saleLinesByProduct[item.ProductId];
            previousLines.TryGetValue(original.Id, out var previous);

            var returnedQuantity = previous?.Quantity ?? 0m;
            var remainingQuantity = original.Quantity - returnedQuantity;

            if (item.Quantity > remainingQuantity)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new
                {
                    message = "Returned quantity exceeds the quantity still returnable.",
                    productId = item.ProductId,
                    remainingQuantity
                });
            }

            decimal subtotal;
            decimal taxAmount;
            decimal total;

            if (item.Quantity == remainingQuantity)
            {
                subtotal = MoneyMath.RoundCurrency(original.Subtotal - (previous?.Subtotal ?? 0m));
                taxAmount = MoneyMath.RoundCurrency(original.TaxAmount - (previous?.TaxAmount ?? 0m));
                total = MoneyMath.RoundCurrency(original.Total - (previous?.Total ?? 0m));
            }
            else
            {
                subtotal = MoneyMath.RoundCurrency(item.Quantity * original.UnitPrice);
                taxAmount = MoneyMath.RoundCurrency(subtotal * original.TaxRate / 100m);
                total = MoneyMath.RoundCurrency(subtotal + taxAmount);
            }

            salesReturn.AddLine(
                original.Id,
                original.ProductId,
                original.ProductName,
                original.Sku,
                item.Quantity,
                original.UnitPrice,
                original.ItbisCategory,
                original.TaxRate,
                subtotal,
                taxAmount,
                total);
        }

        var refundTotal = MoneyMath.RoundCurrency(request.Refunds.Sum(x => x.Amount));
        if (refundTotal != salesReturn.Total)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["refunds"] = [$"Refunds must equal the return total exactly. Expected {salesReturn.Total:F2}, received {refundTotal:F2}."]
            });
        }

        var originalPaidByMethod = await dbContext.Payments
            .Where(x => x.SaleId == sale.Id)
            .GroupBy(x => x.Method)
            .Select(group => new { Method = group.Key, Amount = group.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Method, x => x.Amount, cancellationToken);

        var previouslyRefundedByMethod = await dbContext.Refunds
            .Where(x => x.SaleId == sale.Id)
            .GroupBy(x => x.Method)
            .Select(group => new { Method = group.Key, Amount = group.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.Method, x => x.Amount, cancellationToken);

        foreach (var refundGroup in request.Refunds.GroupBy(x => x.Method))
        {
            var originalPaid = originalPaidByMethod.GetValueOrDefault(refundGroup.Key);
            var previouslyRefunded = previouslyRefundedByMethod.GetValueOrDefault(refundGroup.Key);
            var requested = MoneyMath.RoundCurrency(refundGroup.Sum(x => x.Amount));

            if (requested > MoneyMath.RoundCurrency(originalPaid - previouslyRefunded))
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new
                {
                    message = "Refund amount exceeds the remaining amount paid with this payment method.",
                    method = refundGroup.Key
                });
            }
        }

        foreach (var refundRequest in request.Refunds)
            salesReturn.AddRefund(refundRequest.Method, refundRequest.Amount, refundRequest.Reference);

        var cashRefunds = salesReturn.Refunds.Where(x => x.Method == PaymentMethod.Cash).ToList();
        var cashMovements = new List<CashMovement>();

        if (cashRefunds.Count > 0)
        {
            if (!request.CashSessionId.HasValue)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["cashSessionId"] = ["An open cash session is required for cash refunds."]
                });
            }

            var session = await dbContext.CashSessions
                .FromSqlInterpolated($@"SELECT * FROM cash_sessions WHERE ""Id"" = {request.CashSessionId.Value} FOR UPDATE")
                .SingleOrDefaultAsync(cancellationToken);

            if (session is null || session.ClosedAtUtc is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "The selected cash session is not open." });
            }

            var register = await dbContext.CashRegisters
                .SingleAsync(x => x.Id == session.CashRegisterId, cancellationToken);

            if (!register.IsActive || register.BranchId != sale.BranchId)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "The cash session does not belong to the sale branch." });
            }

            var currentMovementTotal = await dbContext.CashMovements
                .Where(x => x.CashSessionId == session.Id)
                .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

            var cashRefundTotal = MoneyMath.RoundCurrency(cashRefunds.Sum(x => x.Amount));
            var expectedCash = MoneyMath.RoundCurrency(session.OpeningBalance + currentMovementTotal);

            if (cashRefundTotal > expectedCash)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Expected cash in the session is insufficient for this refund." });
            }

            foreach (var refund in cashRefunds)
            {
                cashMovements.Add(CashMovement.FromRefund(
                    currentBusiness.BusinessId.Value,
                    session.Id,
                    userId,
                    sale.Id,
                    refund.Id,
                    refund.Amount));
            }
        }


        var products = await dbContext.Products
            .Where(x => request.Items.Select(item => item.ProductId).Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        var stockMovements = new List<StockMovement>();
        foreach (var line in salesReturn.Lines.OrderBy(x => x.ProductId))
        {
            if (!products[line.ProductId].TracksInventory)
                continue;

            var now = DateTimeOffset.UtcNow;
            var affected = await dbContext.StockBalances
                .Where(x => x.BranchId == sale.BranchId && x.ProductId == line.ProductId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Quantity, x => x.Quantity + line.Quantity)
                        .SetProperty(x => x.UpdatedAtUtc, now),
                    cancellationToken);

            if (affected != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new { message = "Inventory balance for a returned product is unavailable." });
            }

            var balanceAfter = await dbContext.StockBalances
                .Where(x => x.BranchId == sale.BranchId && x.ProductId == line.ProductId)
                .Select(x => x.Quantity)
                .SingleAsync(cancellationToken);

            stockMovements.Add(StockMovement.CreateReturn(
                currentBusiness.BusinessId.Value,
                sale.BranchId,
                line.ProductId,
                userId,
                sale.Id,
                salesReturn.Id,
                line.Quantity,
                balanceAfter));
        }

        dbContext.Returns.Add(salesReturn);
        dbContext.StockMovements.AddRange(stockMovements);
        dbContext.CashMovements.AddRange(cashMovements);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();

            var concurrent = await dbContext.Returns
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, cancellationToken);

            if (concurrent is null)
                throw;

            return await BuildResultAsync(concurrent.Id, dbContext, cancellationToken);
        }

        return await BuildResultAsync(salesReturn.Id, dbContext, cancellationToken, StatusCodes.Status201Created);
    }

    private static Task<IResult> GetAsync(
        Guid returnId,
        BillingDbContext dbContext,
        CancellationToken cancellationToken) =>
        BuildResultAsync(returnId, dbContext, cancellationToken);

    private static async Task<IResult> BuildResultAsync(
        Guid returnId,
        BillingDbContext dbContext,
        CancellationToken cancellationToken,
        int statusCode = StatusCodes.Status200OK)
    {
        var item = await dbContext.Returns.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == returnId, cancellationToken);

        if (item is null)
            return Results.NotFound();

        var lines = await dbContext.ReturnLines.AsNoTracking()
            .Where(x => x.ReturnId == item.Id)
            .OrderBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.SaleLineId,
                x.ProductId,
                x.ProductName,
                x.Sku,
                x.Quantity,
                x.UnitPrice,
                x.ItbisCategory,
                x.TaxRate,
                x.Subtotal,
                x.TaxAmount,
                x.Total
            })
            .ToListAsync(cancellationToken);

        var refunds = await dbContext.Refunds.AsNoTracking()
            .Where(x => x.ReturnId == item.Id)
            .Select(x => new { x.Id, x.Method, x.Amount, x.Reference })
            .ToListAsync(cancellationToken);

        return Results.Json(new
        {
            item.Id,
            item.SaleId,
            item.BranchId,
            item.Reason,
            item.Subtotal,
            item.TaxAmount,
            item.Total,
            item.CreatedAtUtc,
            Lines = lines,
            Refunds = refunds
        }, statusCode: statusCode);
    }

    public sealed record CreateReturnRequest(
        Guid SaleId,
        string Reason,
        List<ReturnItemRequest>? Items,
        List<RefundRequest>? Refunds,
        Guid? CashSessionId = null);

    public sealed record ReturnItemRequest(Guid ProductId, decimal Quantity);
    public sealed record RefundRequest(PaymentMethod Method, decimal Amount, string? Reference);
}
