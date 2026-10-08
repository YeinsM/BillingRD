using System.Security.Claims;
using BillingRD.Api.Security;
using BillingRD.Domain.Adjustments;
using BillingRD.Domain.Customers;
using BillingRD.Domain.ElectronicInvoicing;
using BillingRD.Domain.Identity;
using BillingRD.Infrastructure.Persistence;
using BillingRD.Infrastructure.ElectronicInvoicing;
using Microsoft.EntityFrameworkCore;

namespace BillingRD.Api.Endpoints;

public static class ElectronicInvoicingEndpoints
{
    public static IEndpointRouteBuilder MapElectronicInvoicingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var adjustments = endpoints.MapGroup("/api/adjustments").RequireAuthorization();
        adjustments.MapGet("/{adjustmentId:guid}", GetAdjustmentAsync);

        var drafts = endpoints.MapGroup("/api/electronic-invoicing/drafts").RequireAuthorization();
        drafts.MapPost("/invoice/{invoiceId:guid}", CreateInvoiceDraftAsync);
        drafts.MapPost("/credit-note/from-adjustment/{adjustmentId:guid}", CreateCreditNoteDraftAsync);
        drafts.MapGet("/{draftId:guid}", GetDraftAsync);
        drafts.MapPost("/{draftId:guid}/xml-preview", GenerateXmlPreviewAsync);

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

    private static async Task<IResult> CreateInvoiceDraftAsync(
        Guid invoiceId,
        CreateInvoiceDraftRequest request,
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before preparing an e-CF draft." });

        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Unauthorized();

        if (!await CanPrepareFiscalDraftAsync(dbContext, currentBusiness.BusinessId.Value, userId, cancellationToken))
            return Results.Forbid();

        if (request.Type is not (EcfType.CreditFiscalInvoice31 or EcfType.ConsumerInvoice32))
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["type"] = ["Only CreditFiscalInvoice31 or ConsumerInvoice32 can be prepared from an internal invoice."]
            });
        }

        var existing = await dbContext.ElectronicFiscalDocumentDrafts
            .AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, cancellationToken);

        if (existing is not null)
        {
            if (existing.Type != request.Type)
                return Results.Conflict(new { message = "This invoice already has a fiscal draft of another type." });

            return BuildDraftResult(existing, StatusCodes.Status200OK);
        }

        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == invoiceId, cancellationToken);

        if (invoice is null)
            return Results.NotFound();

        var sale = await dbContext.Sales
            .AsNoTracking()
            .SingleAsync(x => x.Id == invoice.SaleId, cancellationToken);

        var issuer = await dbContext.BusinessFiscalProfiles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.BusinessId == currentBusiness.BusinessId.Value, cancellationToken);

        if (issuer is null)
        {
            return Results.Conflict(new
            {
                message = "Configure the business fiscal profile before preparing e-CF drafts."
            });
        }

        Customer? buyer = null;
        if (invoice.CustomerId.HasValue)
        {
            buyer = await dbContext.Customers
                .AsNoTracking()
                .SingleOrDefaultAsync(x => x.Id == invoice.CustomerId.Value && x.IsActive, cancellationToken);

            if (buyer is null)
                return Results.Conflict(new { message = "The invoice customer is not available." });
        }

        var saleLines = await dbContext.SaleLines
            .AsNoTracking()
            .Where(x => x.SaleId == sale.Id)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var payments = await dbContext.Payments
            .AsNoTracking()
            .Where(x => x.SaleId == sale.Id)
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        var dominicanTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Santo_Domingo");
        var fiscalIssueDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTime(invoice.IssuedAtUtc, dominicanTimeZone).Date);

        ElectronicFiscalDocumentDraft draft;
        try
        {
            draft = ElectronicFiscalDocumentDraft.InvoiceFromSale(
                currentBusiness.BusinessId.Value,
                invoice.Id,
                sale.Id,
                invoice.CustomerId,
                userId,
                request.Type,
                issuer,
                buyer,
                saleLines,
                payments,
                fiscalIssueDate,
                invoice.Subtotal,
                invoice.TaxAmount,
                invoice.Total);
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["draft"] = [ex.Message] });
        }

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
                .SingleOrDefaultAsync(x => x.InvoiceId == invoiceId, cancellationToken);

            if (concurrent is null)
                throw;

            if (concurrent.Type != request.Type)
                return Results.Conflict(new { message = "This invoice already has a fiscal draft of another type." });

            return BuildDraftResult(concurrent, StatusCodes.Status200OK);
        }

        return BuildDraftResult(draft, StatusCodes.Status201Created);
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

        if (!await CanPrepareFiscalDraftAsync(dbContext, currentBusiness.BusinessId.Value, userId, cancellationToken))
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
            .Include(x => x.Lines)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == draftId, cancellationToken);

        return draft is null
            ? Results.NotFound()
            : BuildDraftResult(draft, StatusCodes.Status200OK);
    }

    private static async Task<IResult> GenerateXmlPreviewAsync(
        Guid draftId,
        XmlPreviewRequest request,
        HttpContext httpContext,
        CurrentBusinessContext currentBusiness,
        BillingDbContext dbContext,
        CancellationToken cancellationToken)
    {
        if (!currentBusiness.BusinessId.HasValue)
            return Results.Conflict(new { message = "Select an active business before generating an XML preview." });

        if (!Guid.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
            return Results.Unauthorized();

        if (!await CanPrepareFiscalDraftAsync(dbContext, currentBusiness.BusinessId.Value, userId, cancellationToken))
            return Results.Forbid();

        var draft = await dbContext.ElectronicFiscalDocumentDrafts
            .AsNoTracking()
            .Include(x => x.Lines)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(x => x.Id == draftId, cancellationToken);

        if (draft is null)
            return Results.NotFound();

        try
        {
            var xml = DgiiEcfXmlPreviewGenerator.Generate(draft, request.ENcf, request.SequenceExpiration);
            return Results.Ok(new
            {
                draft.Id,
                ecfType = (int)draft.Type,
                previewOnly = true,
                schemaValidation = "preflight-only",
                digitallySigned = false,
                submittableToDgii = false,
                xml
            });
        }
        catch (ArgumentException ex)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["xmlPreview"] = [ex.Message] });
        }
        catch (InvalidOperationException ex)
        {
            return Results.Conflict(new { message = ex.Message });
        }
    }

    private static Task<bool> CanPrepareFiscalDraftAsync(
        BillingDbContext dbContext,
        Guid businessId,
        Guid userId,
        CancellationToken cancellationToken) =>
        dbContext.BusinessMemberships
            .IgnoreQueryFilters()
            .Where(x => x.BusinessId == businessId && x.UserId == userId)
            .Select(x => x.Role)
            .AnyAsync(role => role == BusinessRole.Owner || role == BusinessRole.Administrator, cancellationToken);

    private static IResult BuildDraftResult(ElectronicFiscalDocumentDraft draft, int statusCode) =>
        Results.Json(new
        {
            draft.Id,
            draft.InvoiceId,
            draft.AdjustmentDocumentId,
            draft.SaleId,
            draft.ReturnId,
            draft.CustomerId,
            ecfType = (int)draft.Type,
            ecfTypeName = draft.Type.ToString(),
            issuer = new
            {
                rnc = draft.IssuerRnc,
                legalName = draft.IssuerLegalName,
                tradeName = draft.IssuerTradeName,
                address = draft.IssuerAddress
            },
            buyer = new
            {
                taxId = draft.BuyerTaxId,
                foreignIdentifier = draft.BuyerForeignIdentifier,
                name = draft.BuyerName,
                address = draft.BuyerAddress
            },
            draft.FiscalIssueDate,
            draft.IncomeType,
            draft.PaymentType,
            totals = new
            {
                draft.TaxableAmount18,
                draft.TaxableAmount16,
                draft.TaxableAmount0,
                draft.ExemptAmount,
                draft.Tax18,
                draft.Tax16,
                draft.Subtotal,
                draft.TaxAmount,
                draft.Total
            },
            lines = draft.Lines
                .OrderBy(x => x.Number)
                .Select(x => new
                {
                    x.Number,
                    x.ProductId,
                    x.Sku,
                    x.Name,
                    x.ProductKind,
                    x.BillingIndicator,
                    x.Quantity,
                    x.UnitPrice,
                    x.Amount,
                    x.TaxAmount,
                    x.TaxRate
                }),
            payments = draft.Payments
                .Select(x => new { x.FormCode, x.Amount }),
            draft.CreatedAtUtc,
            issued = false,
            eNcf = (string?)null,
            xmlGenerated = false,
            digitallySigned = false,
            submittedToDgii = false
        }, statusCode: statusCode);

    public sealed record CreateInvoiceDraftRequest(EcfType Type);
    public sealed record XmlPreviewRequest(string ENcf, DateOnly? SequenceExpiration);
}
