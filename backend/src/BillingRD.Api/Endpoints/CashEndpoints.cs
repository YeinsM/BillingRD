using System.Security.Claims;
using BillingRD.Api.Security;
using BillingRD.Domain.Billing;
using BillingRD.Domain.Cash;
using BillingRD.Domain.Identity;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Endpoints;

public static class CashEndpoints
{
    public static IEndpointRouteBuilder MapCashEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var registers = endpoints.MapGroup("/api/cash-registers").RequireAuthorization();
        registers.MapGet("/", ListRegistersAsync);
        registers.MapPost("/", CreateRegisterAsync);

        var sessions = endpoints.MapGroup("/api/cash-sessions").RequireAuthorization();
        sessions.MapPost("/open", OpenSessionAsync);
        sessions.MapGet("/{sessionId:guid}", GetSessionAsync);
        sessions.MapPost("/{sessionId:guid}/movements", AddManualMovementAsync);
        sessions.MapPost("/{sessionId:guid}/close", CloseSessionAsync);

        return endpoints;
    }

    private static async Task<IResult> ListRegistersAsync(
        Guid branchId,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before accessing cash registers." });

        if (!await dbContext.Branches.AnyAsync(x => x.Id == branchId && x.IsActive, cancellationToken))
            return Results.NotFound();

        var registers = await dbContext.CashRegisters
            .Where(x => x.BranchId == branchId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new { x.Id, x.BranchId, x.Name, x.IsActive })
            .ToListAsync(cancellationToken);

        return Results.Ok(registers);
    }

    private static async Task<IResult> CreateRegisterAsync(
        CreateRegisterRequest request,
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before creating a cash register." });

        if (!TryGetUserId(httpContext, out var userId)) return Results.Unauthorized();

        var role = await GetRoleAsync(dbContext, currentBusiness.BusinessId.Value, userId, cancellationToken);
        if (role is not (BusinessRole.Owner or BusinessRole.Administrator)) return Results.Forbid();

        if (request.BranchId == Guid.Empty || string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 120)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["cashRegister"] = ["Branch and a name up to 120 characters are required."]
            });

        if (!await dbContext.Branches.AnyAsync(x => x.Id == request.BranchId && x.IsActive, cancellationToken))
            return Results.NotFound();

        var register = CashRegister.Create(currentBusiness.BusinessId.Value, request.BranchId, request.Name);
        dbContext.CashRegisters.Add(register);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { message = "A cash register with this name already exists in the branch." });
        }

        return Results.Created($"/api/cash-registers/{register.Id}", new
        {
            register.Id,
            register.BranchId,
            register.Name
        });
    }

    private static async Task<IResult> OpenSessionAsync(
        OpenSessionRequest request,
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before opening a cash session." });

        if (!TryGetUserId(httpContext, out var userId)) return Results.Unauthorized();

        var role = await GetRoleAsync(dbContext, currentBusiness.BusinessId.Value, userId, cancellationToken);
        if (role is not (BusinessRole.Owner or BusinessRole.Administrator or BusinessRole.Cashier))
            return Results.Forbid();

        if (request.CashRegisterId == Guid.Empty || request.OpeningBalance < 0)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["cashSession"] = ["Cash register and non-negative opening balance are required."]
            });

        var register = await dbContext.CashRegisters.SingleOrDefaultAsync(
            x => x.Id == request.CashRegisterId && x.IsActive,
            cancellationToken);

        if (register is null) return Results.NotFound();

        var session = CashSession.Open(
            currentBusiness.BusinessId.Value,
            register.Id,
            userId,
            request.OpeningBalance);

        dbContext.CashSessions.Add(session);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            return Results.Conflict(new { message = "This cash register already has an open session." });
        }

        return Results.Created($"/api/cash-sessions/{session.Id}", new
        {
            session.Id,
            session.CashRegisterId,
            session.OpeningBalance,
            session.OpenedAtUtc
        });
    }

    private static async Task<IResult> GetSessionAsync(
        Guid sessionId,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before accessing cash sessions." });

        var session = await dbContext.CashSessions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == sessionId, cancellationToken);

        if (session is null) return Results.NotFound();

        var movementsTotal = await dbContext.CashMovements
            .Where(x => x.CashSessionId == session.Id)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var expectedNow = MoneyMath.RoundCurrency(session.OpeningBalance + movementsTotal);

        return Results.Ok(new
        {
            session.Id,
            session.CashRegisterId,
            session.OpenedByUserId,
            session.OpeningBalance,
            session.OpenedAtUtc,
            session.ClosedByUserId,
            session.ClosedAtUtc,
            expectedCash = session.ExpectedCashAtClose ?? expectedNow,
            session.CountedCash,
            session.Difference,
            isOpen = session.ClosedAtUtc is null
        });
    }

    private static async Task<IResult> AddManualMovementAsync(
        Guid sessionId,
        ManualMovementRequest request,
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before moving cash." });

        if (!TryGetUserId(httpContext, out var userId)) return Results.Unauthorized();

        var role = await GetRoleAsync(dbContext, currentBusiness.BusinessId.Value, userId, cancellationToken);
        if (role is not (BusinessRole.Owner or BusinessRole.Administrator))
            return Results.Forbid();

        if (request.Type is not (CashMovementType.ManualIncome or CashMovementType.Expense or CashMovementType.Withdrawal) ||
            request.Amount <= 0 ||
            string.IsNullOrWhiteSpace(request.Reason) ||
            request.Reason.Length > 240)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["movement"] = ["ManualIncome, Expense or Withdrawal with positive amount and reason up to 240 characters is required."]
            });
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var session = await dbContext.CashSessions
            .FromSqlInterpolated($@"SELECT * FROM cash_sessions WHERE ""Id"" = {sessionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.NotFound();
        }

        if (session.ClosedAtUtc is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new { message = "Cash session is closed." });
        }

        var movement = CashMovement.Manual(
            currentBusiness.BusinessId.Value,
            session.Id,
            userId,
            request.Type,
            request.Amount,
            request.Reason);

        var previousMovements = await dbContext.CashMovements
            .Where(x => x.CashSessionId == session.Id)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var expectedAfter = MoneyMath.RoundCurrency(session.OpeningBalance + previousMovements + movement.Amount);
        if (expectedAfter < 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new { message = "This movement would make expected cash negative." });
        }

        dbContext.CashMovements.Add(movement);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Ok(new
        {
            movement.Id,
            movement.Type,
            movement.Amount,
            expectedCash = expectedAfter
        });
    }

    private static async Task<IResult> CloseSessionAsync(
        Guid sessionId,
        CloseSessionRequest request,
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before closing a cash session." });

        if (!TryGetUserId(httpContext, out var userId)) return Results.Unauthorized();

        var role = await GetRoleAsync(dbContext, currentBusiness.BusinessId.Value, userId, cancellationToken);
        if (role is not (BusinessRole.Owner or BusinessRole.Administrator or BusinessRole.Cashier))
            return Results.Forbid();

        if (request.CountedCash < 0)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["countedCash"] = ["Counted cash cannot be negative."]
            });

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var session = await dbContext.CashSessions
            .FromSqlInterpolated($@"SELECT * FROM cash_sessions WHERE ""Id"" = {sessionId} FOR UPDATE")
            .SingleOrDefaultAsync(cancellationToken);

        if (session is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.NotFound();
        }

        if (session.ClosedAtUtc is not null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Results.Conflict(new { message = "Cash session is already closed." });
        }

        var movements = await dbContext.CashMovements
            .Where(x => x.CashSessionId == session.Id)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var expected = MoneyMath.RoundCurrency(session.OpeningBalance + movements);
        session.Close(userId, expected, request.CountedCash);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return Results.Ok(new
        {
            session.Id,
            expectedCash = session.ExpectedCashAtClose,
            countedCash = session.CountedCash,
            difference = session.Difference,
            session.ClosedAtUtc
        });
    }

    private static bool TryGetUserId(HttpContext httpContext, out Guid userId) =>
        Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static Task<BusinessRole> GetRoleAsync(
        BillingDbContext dbContext,
        Guid businessId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.BusinessMemberships
            .IgnoreQueryFilters()
            .Where(x => x.BusinessId == businessId && x.UserId == userId)
            .Select(x => x.Role)
            .SingleOrDefaultAsync(cancellationToken);

    public sealed record CreateRegisterRequest(Guid BranchId, string Name);
    public sealed record OpenSessionRequest(Guid CashRegisterId, decimal OpeningBalance);
    public sealed record ManualMovementRequest(CashMovementType Type, decimal Amount, string Reason);
    public sealed record CloseSessionRequest(decimal CountedCash);
}
