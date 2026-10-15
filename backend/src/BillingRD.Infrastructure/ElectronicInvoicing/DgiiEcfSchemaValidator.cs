using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using BillingRD.Domain.ElectronicInvoicing;

namespace BillingRD.Infrastructure.ElectronicInvoicing;

public static class DgiiEcfSchemaValidator
{
    private sealed record SchemaDescriptor(
        EcfType Type,
        string FileName,
        string ExpectedSha256,
        bool NormalizeKnownWhitespaceDefect);

    public sealed record ValidationResult(
        bool IsValid,
        string SchemaSha256,
        bool OfficialSchemaNormalizedInMemory,
        IReadOnlyList<string> Errors);

    private static readonly IReadOnlyDictionary<EcfType, SchemaDescriptor> Schemas =
        new Dictionary<EcfType, SchemaDescriptor>
        {
            [EcfType.CreditFiscalInvoice31] = new(
                EcfType.CreditFiscalInvoice31,
                "ecf-31-v1.0.official.xsd",
                "6f2909a93d84919518d2ae3c77fead4b35c3e8c95996b8af67b0040c2e2be298",
                true),
            [EcfType.ConsumerInvoice32] = new(
                EcfType.ConsumerInvoice32,
                "ecf-32-v1.0.official.xsd",
                "151676df03c61335b4ac1eca97f99395488d57ea92793b49353602871e651f3c",
                false)
        };

    public static ValidationResult Validate(EcfType type, string xml)
    {
        if (!Schemas.TryGetValue(type, out var descriptor))
            throw new ArgumentOutOfRangeException(nameof(type), type, "Only e-CF 31 and 32 XSD validation is supported.");

        ArgumentException.ThrowIfNullOrWhiteSpace(xml);

        var (schemaText, actualHash) = LoadAndVerifySchema(descriptor);
        var normalized = false;

        if (descriptor.NormalizeKnownWhitespaceDefect)
        {
            const string invalid = "name=\" IndicadorServicioTodoIncluidoType\"";
            const string corrected = "name=\"IndicadorServicioTodoIncluidoType\"";

            if (!schemaText.Contains(invalid, StringComparison.Ordinal))
                throw new InvalidOperationException("The pinned e-CF 31 XSD no longer contains the expected DGII whitespace defect. Review the schema before updating validation behavior.");

            schemaText = schemaText.Replace(invalid, corrected, StringComparison.Ordinal);
            normalized = true;
        }

        var errors = new List<string>();
        var schemaSet = new XmlSchemaSet();
        schemaSet.ValidationEventHandler += (_, args) => errors.Add($"Schema: {args.Message}");

        using (var schemaReader = XmlReader.Create(
                   new StringReader(schemaText),
                   new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit }))
        {
            var schema = XmlSchema.Read(schemaReader, (_, args) => errors.Add($"Schema: {args.Message}"))
                ?? throw new InvalidOperationException("DGII XSD could not be parsed.");
            schemaSet.Add(schema);
        }

        try
        {
            schemaSet.Compile();
        }
        catch (XmlSchemaException ex)
        {
            errors.Add($"Schema compile: {ex.Message}");
        }

        if (errors.Count > 0)
            return new ValidationResult(false, actualHash, normalized, errors);

        var settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            ValidationType = ValidationType.Schema,
            Schemas = schemaSet
        };
        settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
        settings.ValidationEventHandler += (_, args) => errors.Add(args.Message);

        try
        {
            using var stringReader = new StringReader(xml);
            using var xmlReader = XmlReader.Create(stringReader, settings);
            while (xmlReader.Read()) { }
        }
        catch (XmlException ex)
        {
            errors.Add($"XML: {ex.Message}");
        }
        catch (XmlSchemaValidationException ex)
        {
            errors.Add($"Validation: {ex.Message}");
        }

        return new ValidationResult(errors.Count == 0, actualHash, normalized, errors);
    }

    private static (string Text, string Sha256) LoadAndVerifySchema(SchemaDescriptor descriptor)
    {
        var assembly = typeof(DgiiEcfSchemaValidator).Assembly;
        var resourceName = assembly
            .GetManifestResourceNames()
            .SingleOrDefault(name => name.EndsWith(descriptor.FileName, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded DGII XSD '{descriptor.FileName}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded DGII XSD '{descriptor.FileName}' could not be opened.");
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        var bytes = memory.ToArray();

        var actualHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        if (!string.Equals(actualHash, descriptor.ExpectedSha256, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"DGII XSD integrity mismatch for {descriptor.FileName}. Expected {descriptor.ExpectedSha256}, got {actualHash}.");
        }

        return (Encoding.UTF8.GetString(bytes), actualHash);
    }
}
