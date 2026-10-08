using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using BillingRD.Domain.ElectronicInvoicing;

namespace BillingRD.Infrastructure.ElectronicInvoicing;

public static partial class DgiiEcfXmlPreviewGenerator
{
    public static string Generate(
        ElectronicFiscalDocumentDraft draft,
        string eNcf,
        DateOnly? sequenceExpiration)
    {
        if (draft.Type is not (EcfType.CreditFiscalInvoice31 or EcfType.ConsumerInvoice32))
            throw new InvalidOperationException("XML preview currently supports only e-CF 31 and 32.");

        if (draft.FiscalIssueDate is null ||
            string.IsNullOrWhiteSpace(draft.IssuerRnc) ||
            string.IsNullOrWhiteSpace(draft.IssuerLegalName) ||
            string.IsNullOrWhiteSpace(draft.IssuerAddress))
            throw new InvalidOperationException("The fiscal draft is incomplete.");

        ValidateENcf(draft.Type, eNcf);

        if (draft.Type == EcfType.CreditFiscalInvoice31 && sequenceExpiration is null)
            throw new InvalidOperationException("e-CF 31 requires sequence expiration date.");

        if (draft.Lines.Count == 0)
            throw new InvalidOperationException("The fiscal draft has no detail lines.");

        if (draft.Payments.Count == 0 || draft.Payments.Count > 7)
            throw new InvalidOperationException("The fiscal draft must contain between 1 and 7 payment forms.");

        var idDoc = new XElement("IdDoc",
            new XElement("TipoeCF", ((int)draft.Type).ToString(CultureInfo.InvariantCulture)),
            new XElement("eNCF", eNcf));

        if (draft.Type == EcfType.CreditFiscalInvoice31 && sequenceExpiration.HasValue)
            idDoc.Add(new XElement("FechaVencimientoSecuencia", FormatDate(sequenceExpiration.Value)));

        if (draft.TaxableAmount18 + draft.TaxableAmount16 + draft.TaxableAmount0 > 0)
            idDoc.Add(new XElement("IndicadorMontoGravado", "0"));

        idDoc.Add(
            new XElement("TipoIngresos", draft.IncomeType),
            new XElement("TipoPago", draft.PaymentType.ToString(CultureInfo.InvariantCulture)),
            new XElement("TablaFormasPago",
                draft.Payments
                    .OrderBy(x => x.FormCode)
                    .ThenBy(x => x.Id)
                    .Select(x => new XElement("FormaDePago",
                        new XElement("FormaPago", x.FormCode.ToString(CultureInfo.InvariantCulture)),
                        new XElement("MontoPago", Money(x.Amount))))));

        var emisor = new XElement("Emisor",
            new XElement("RNCEmisor", draft.IssuerRnc),
            new XElement("RazonSocialEmisor", draft.IssuerLegalName));

        if (!string.IsNullOrWhiteSpace(draft.IssuerTradeName))
            emisor.Add(new XElement("NombreComercial", draft.IssuerTradeName));

        emisor.Add(
            new XElement("DireccionEmisor", draft.IssuerAddress),
            new XElement("FechaEmision", FormatDate(draft.FiscalIssueDate.Value)));

        var comprador = new XElement("Comprador");
        if (!string.IsNullOrWhiteSpace(draft.BuyerTaxId))
            comprador.Add(new XElement("RNCComprador", draft.BuyerTaxId));
        if (!string.IsNullOrWhiteSpace(draft.BuyerForeignIdentifier))
            comprador.Add(new XElement("IdentificadorExtranjero", draft.BuyerForeignIdentifier));
        if (!string.IsNullOrWhiteSpace(draft.BuyerName))
            comprador.Add(new XElement("RazonSocialComprador", draft.BuyerName));
        if (!string.IsNullOrWhiteSpace(draft.BuyerAddress))
            comprador.Add(new XElement("DireccionComprador", draft.BuyerAddress));

        var totales = new XElement("Totales");
        var taxableTotal = draft.TaxableAmount18 + draft.TaxableAmount16 + draft.TaxableAmount0;

        if (taxableTotal > 0)
            totales.Add(new XElement("MontoGravadoTotal", Money(taxableTotal)));
        if (draft.TaxableAmount18 > 0)
        {
            totales.Add(
                new XElement("MontoGravadoI1", Money(draft.TaxableAmount18)),
                new XElement("ITBIS1", "18"),
                new XElement("TotalITBIS1", Money(draft.Tax18)));
        }
        if (draft.TaxableAmount16 > 0)
        {
            totales.Add(
                new XElement("MontoGravadoI2", Money(draft.TaxableAmount16)),
                new XElement("ITBIS2", "16"),
                new XElement("TotalITBIS2", Money(draft.Tax16)));
        }
        if (draft.TaxableAmount0 > 0)
        {
            totales.Add(
                new XElement("MontoGravadoI3", Money(draft.TaxableAmount0)),
                new XElement("ITBIS3", "0"));
        }
        if (draft.ExemptAmount > 0)
            totales.Add(new XElement("MontoExento", Money(draft.ExemptAmount)));
        if (draft.TaxAmount > 0)
            totales.Add(new XElement("TotalITBIS", Money(draft.TaxAmount)));

        totales.Add(new XElement("MontoTotal", Money(draft.Total)));

        var detalles = new XElement("DetallesItems",
            draft.Lines
                .OrderBy(x => x.Number)
                .Select(line => new XElement("Item",
                    new XElement("NumeroLinea", line.Number),
                    new XElement("TablaCodigosItem",
                        new XElement("CodigosItem",
                            new XElement("TipoCodigo", "Interna"),
                            new XElement("CodigoItem", line.Sku))),
                    new XElement("IndicadorFacturacion", line.BillingIndicator),
                    new XElement("NombreItem", line.Name),
                    new XElement("IndicadorBienoServicio", (int)line.ProductKind),
                    new XElement("CantidadItem", Quantity(line.Quantity)),
                    new XElement("PrecioUnitarioItem", UnitPrice(line.UnitPrice)),
                    new XElement("MontoItem", Money(line.Amount)))));

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("ECF",
                new XElement("Encabezado",
                    new XElement("Version", "1.0"),
                    idDoc,
                    emisor,
                    comprador,
                    totales),
                detalles));

        return document.ToString(SaveOptions.DisableFormatting);
    }

    private static void ValidateENcf(EcfType type, string eNcf)
    {
        if (string.IsNullOrWhiteSpace(eNcf) || !ENcfRegex().IsMatch(eNcf))
            throw new ArgumentException("e-NCF preview must match E + type + 10 digits.", nameof(eNcf));

        var expectedPrefix = $"E{(int)type}";
        if (!eNcf.StartsWith(expectedPrefix, StringComparison.Ordinal))
            throw new ArgumentException($"e-NCF preview must start with {expectedPrefix}.", nameof(eNcf));
    }

    private static string FormatDate(DateOnly value) => value.ToString("dd-MM-yyyy", CultureInfo.InvariantCulture);
    private static string Money(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string Quantity(decimal value) => value.ToString("0.00", CultureInfo.InvariantCulture);
    private static string UnitPrice(decimal value) => value.ToString("0.0000", CultureInfo.InvariantCulture);

    [GeneratedRegex("^E(?:31|32)\\d{10}$", RegexOptions.CultureInvariant)]
    private static partial Regex ENcfRegex();
}
