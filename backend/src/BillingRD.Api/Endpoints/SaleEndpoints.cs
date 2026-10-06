using System.Security.Claims;
using BillingRD.Api.Security;
using BillingRD.Domain.Billing;
using BillingRD.Domain.Identity;
using BillingRD.Domain.Inventory;
using BillingRD.Domain.Payments;
using BillingRD.Domain.Sales;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Endpoints;

public static class SaleEndpoints
{
    public static IEndpointRouteBuilder MapSaleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/sales").RequireAuthorization();

        group.MapPost("/", CreateAsync);
        group.MapGet("/{saleId:guid}", GetAsync);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateSaleRequest request,
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
        {
            return Results.Conflict(new { message = "Select an active business before creating a sale." });
        }

        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return Results.Unauthorized();
        }

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

        var existingSale = await dbContext.Sales
            .AsNoTracking()
            .SingleOrDefaultAsync(sale => sale.IdempotencyKey == idempotencyKey, cancellationToken);

        if (existingSale is not null)
        {
            return await BuildSaleResultAsync(existingSale.Id, dbContext, cancellationToken);
        }

        var authorizedRole = await dbContext.BusinessMemberships
            .IgnoreQueryFilters()
            .Where(membership =>
                membership.BusinessId == currentBusiness.BusinessId.Value &&
                membership.UserId == userId)
            .Select(membership => membership.Role)
            .SingleOrDefaultAsync(cancellationToken);

        if (authorizedRole is not (BusinessRole.Owner or BusinessRole.Administrator or BusinessRole.Cashier))
        {
            return Results.Forbid();
        }

        if (request.Items is null || request.Items.Count == 0 ||
            request.Payments is null || request.Payments.Count == 0)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["sale"] = ["At least one item and one payment are required."]
            });
        }

        if (request.Items.Any(item => item.ProductId == Guid.Empty || item.Quantity <= 0) ||
            request.Items.Select(item => item.ProductId).Distinct().Count() != request.Items.Count)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["Each product must appear once and quantities must be greater than zero."]
            });
        }

        var branchExists = await dbContext.Branches
            .AnyAsync(branch => branch.Id == request.BranchId && branch.IsActive, cancellationToken);

        if (!branchExists)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["branchId"] = ["The branch does not belong to the active business or is inactive."]
            });
        }

        if (request.CustomerId.HasValue)
        {
            var customerExists = await dbContext.Customers.AnyAsync(
                customer => customer.Id == request.CustomerId.Value && customer.IsActive,
                cancellationToken);

            if (!customerExists)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["customerId"] = ["The customer does not belong to the active business or is inactive."]
                });
            }
        }

        var productIds = request.Items.Select(item => item.ProductId).ToArray();
        var products = await dbContext.Products
            .Where(product => productIds.Contains(product.Id) && product.IsActive)
            .ToDictionaryAsync(product => product.Id, cancellationToken);

        if (products.Count != productIds.Length)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["items"] = ["One or more products do not belong to the active business or are inactive."]
            });
        }

        var sale = Sale.Create(
            currentBusiness.BusinessId.Value,
            request.BranchId,
            request.CustomerId,
            userId,
            idempotencyKey);

        foreach (var item in request.Items)
        {
            var product = products[item.ProductId];
            sale.AddLine(
                product.Id,
                product.Name,
                product.Sku,
                item.Quantity,
                product.SalePrice,
                product.ItbisCategory);
        }

        var payments = new List<Payment>(request.Payments.Count);
        foreach (var paymentRequest in request.Payments)
        {
            if (!Enum.IsDefined(paymentRequest.Method) || paymentRequest.Amount <= 0)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["payments"] = ["Every payment must have a supported method and an amount greater than zero."]
                });
            }

            payments.Add(Payment.Create(
                currentBusiness.BusinessId.Value,
                sale.Id,
                paymentRequest.Method,
                paymentRequest.Amount,
                paymentRequest.Reference));
        }

        var paidAmount = MoneyMath.RoundCurrency(payments.Sum(payment => payment.Amount));
        if (paidAmount != sale.Total)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["payments"] = [$"Payments must equal the sale total exactly. Expected {sale.Total:F2}, received {paidAmount:F2}."]
            });
        }

        var invoice = Invoice.Issue(
            currentBusiness.BusinessId.Value,
            sale.Id,
            request.CustomerId,
            sale.Subtotal,
            sale.TaxAmount,
            sale.Total);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        var stockMovements = new List<StockMovement>();

        // Always lock/update products in deterministic order to reduce deadlock risk between concurrent POS sales.
        foreach (var item in request.Items.OrderBy(item => item.ProductId))
        {
            var product = products[item.ProductId];

            if (!product.TracksInventory)
            {
                continue;
            }

            var now = DateTimeOffset.UtcNow;
            var affected = await dbContext.StockBalances
                .Where(balance =>
                    balance.BranchId == request.BranchId &&
                    balance.ProductId == product.Id &&
                    balance.Quantity >= item.Quantity)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(balance => balance.Quantity, balance => balance.Quantity - item.Quantity)
                        .SetProperty(balance => balance.UpdatedAtUtc, now),
                    cancellationToken);

            if (affected != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return Results.Conflict(new
                {
                    message = "Insufficient stock for one or more products.",
                    productId = product.Id
                });
            }

            var balanceAfter = await dbContext.StockBalances
                .Where(balance => balance.BranchId == request.BranchId && balance.ProductId == product.Id)
                .Select(balance => balance.Quantity)
                .SingleAsync(cancellationToken);

            stockMovements.Add(StockMovement.CreateSale(
                currentBusiness.BusinessId.Value,
                request.BranchId,
                product.Id,
                userId,
                sale.Id,
                item.Quantity,
                balanceAfter));
        }

        dbContext.Sales.Add(sale);
        dbContext.Invoices.Add(invoice);
        dbContext.Payments.AddRange(payments);
        dbContext.StockMovements.AddRange(stockMovements);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            await transaction.RollbackAsync(cancellationToken);
            dbContext.ChangeTracker.Clear();

            var concurrentSale = await dbContext.Sales
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.IdempotencyKey == idempotencyKey, cancellationToken);

            if (concurrentSale is null)
            {
                throw;
            }

            return await BuildSaleResultAsync(concurrentSale.Id, dbContext, cancellationToken);
        }

        return await BuildSaleResultAsync(sale.Id, dbContext, cancellationToken, StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetAsync(
        Guid saleId,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        return await BuildSaleResultAsync(saleId, dbContext, cancellationToken);
    }

    private static async Task<IResult> BuildSaleResultAsync(
        Guid saleId,
        BillingDbContext dbContext,
        CancellationToken cancellationToken,
        int statusCode = StatusCodes.Status200OK)
    {
        var sale = await dbContext.Sales
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == saleId, cancellationToken);

        if (sale is null)
        {
            return Results.NotFound();
        }

        var lines = await dbContext.SaleLines
            .AsNoTracking()
            .Where(line => line.SaleId == sale.Id)
            .OrderBy(line => line.Id)
            .Select(line => new
            {
                line.ProductId,
                line.ProductName,
                line.Sku,
                line.Quantity,
                line.UnitPrice,
                line.ItbisCategory,
                line.TaxRate,
                line.Subtotal,
                line.TaxAmount,
                line.Total
            })
            .ToListAsync(cancellationToken);

        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .SingleAsync(item => item.SaleId == sale.Id, cancellationToken);

        var payments = await dbContext.Payments
            .AsNoTracking()
            .Where(payment => payment.SaleId == sale.Id)
            .Select(payment => new
            {
                payment.Id,
                payment.Method,
                payment.Amount,
                payment.Reference
            })
            .ToListAsync(cancellationToken);

        var response = new
        {
            sale.Id,
            sale.BranchId,
            sale.CustomerId,
            sale.Subtotal,
            sale.TaxAmount,
            sale.Total,
            sale.CreatedAtUtc,
            Lines = lines,
            Invoice = new
            {
                invoice.Id,
                invoice.Subtotal,
                invoice.TaxAmount,
                invoice.Total,
                invoice.IssuedAtUtc
            },
            Payments = payments
        };

        return Results.Json(response, statusCode: statusCode);
    }

    public sealed record CreateSaleRequest(
        Guid BranchId,
        Guid? CustomerId,
        List<SaleItemRequest>? Items,
        List<SalePaymentRequest>? Payments);

    public sealed record SaleItemRequest(Guid ProductId, decimal Quantity);
    public sealed record SalePaymentRequest(PaymentMethod Method, decimal Amount, string? Reference);
}
