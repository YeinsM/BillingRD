using System.Security.Claims;
using BillingRD.Api.Security;
using BillingRD.Domain.Adjustments;
using BillingRD.Domain.ElectronicInvoicing;
using BillingRD.Domain.Identity;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Endpoints;

public static class ElectronicInvoicingEndpoints
{
    public static IEndpointRouteBuilder MapElectronicInvoicingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var adjustments = endpoints.MapGroup("/api/adjustments").RequireAuthorization();
        adjustments.MapGet("/{adjustmentId:guid}", GetAdjustmentAsync);

        var drafts = endpoints.MapGroup("/api/electronic-invoicing/drafts").RequireAuthorization();
        drafts.MapPost("/credit-note/from-adjustment/{adjustmentId:guid}", CreateCreditNoteDraftAsync);
        drafts.MapGet("/{draftId:guid}", GetDraftAsync);

        return endpoints;
    }

    private static async Task<IResult> GetAdjustmentAsync(
        Guid adjustmentId,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before accessing adjustments." });

        var adjustment = await dbContext.AdjustmentDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == adjustmentId, cancellationToken);

        if (adjustment is null)
            return Results.NotFound();

        var draftId = await dbContext.ElectronicFiscalDocumentDrafts
            .Where(x => x.AdjustmentDocumentId == adjustment.Id)
            .Select(x => (Guid?)x.Id)
            .SingleOrDefaultAsync(cancellationToken);

        return Results.Ok(new
        {
            adjustment.Id,
            adjustment.SaleId,
            adjustment.ReturnId,
            adjustment.Kind,
            adjustment.Reason,
            adjustment.Subtotal,
            adjustment.TaxAmount,
            adjustment.Total,
            adjustment.CreatedAtUtc,
            electronicFiscalDraftId = draftId
        });
    }

    private static async Task<IResult> CreateCreditNoteDraftAsync(
        Guid adjustmentId,
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before preparing an e-CF draft." });

        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Unauthorized();

        var role = await dbContext.BusinessMemberships
            .IgnoreQueryFilters()
            .Where(x => x.BusinessId == currentBusiness.BusinessId.Value && x.UserId == userId)
            .Select(x => x.Role)
            .SingleOrDefaultAsync(cancellationToken);

        if (role is not (BusinessRole.Owner or BusinessRole.Administrator))
            return Results.Forbid();

        var existing = await dbContext.ElectronicFiscalDocumentDrafts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.AdjustmentDocumentId == adjustmentId, cancellationToken);

        if (existing is not null)
            return BuildDraftResult(existing, StatusCodes.Status200OK);

        var adjustment = await dbContext.AdjustmentDocuments
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == adjustmentId, cancellationToken);

        if (adjustment is null)
            return Results.NotFound();

        if (adjustment.Kind != AdjustmentKind.Credit)
            return Results.Conflict(new { message = "Only credit adjustments are supported by this e-CF draft flow." });

        var sourceReturnExists = await dbContext.Returns
            .AnyAsync(x => x.Id == adjustment.ReturnId && x.SaleId == adjustment.SaleId, cancellationToken);

        if (!sourceReturnExists)
            return Results.Conflict(new { message = "The adjustment does not reference a valid return." });

        var draft = ElectronicFiscalDocumentDraft.CreditNoteFromAdjustment(
            currentBusiness.BusinessId.Value,
            adjustment.Id,
            adjustment.SaleId,
            adjustment.ReturnId,
            userId,
            adjustment.Subtotal,
            adjustment.TaxAmount,
            adjustment.Total);

        dbContext.ElectronicFiscalDocumentDrafts.Add(draft);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            dbContext.ChangeTracker.Clear();

            var concurrent = await dbContext.ElectronicFiscalDocumentDrafts
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.AdjustmentDocumentId == adjustmentId, cancellationToken);

            if (concurrent is null)
                throw;

            return BuildDraftResult(concurrent, StatusCodes.Status200OK);
        }

        return BuildDraftResult(draft, StatusCodes.Status201Created);
    }

    private static async Task<IResult> GetDraftAsync(
        Guid draftId,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before accessing e-CF drafts." });

        var draft = await dbContext.ElectronicFiscalDocumentDrafts
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == draftId, cancellationToken);

        return draft is null
            ? Results.NotFound()
            : BuildDraftResult(draft, StatusCodes.Status200OK);
    }

    private static IResult BuildDraftResult(ElectronicFiscalDocumentDraft draft, int statusCode) =>
        Results.Json(new
        {
            draft.Id,
            draft.AdjustmentDocumentId,
            draft.SaleId,
            draft.ReturnId,
            ecfType = (int)draft.Type,
            ecfTypeName = draft.Type.ToString(),
            draft.Subtotal,
            draft.TaxAmount,
            draft.Total,
            draft.CreatedAtUtc,
            issued = false,
            eNcf = (string?)null,
            xmlGenerated = false,
            digitallySigned = false,
            submittedToDgii = false
        }, statusCode: statusCode);
}
