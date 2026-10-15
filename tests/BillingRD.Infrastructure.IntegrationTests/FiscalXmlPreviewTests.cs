using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Xml.Linq;
using BillingRD.Infrastructure.Persistence;
using BillingRD.Infrastructure.ElectronicInvoicing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BillingRD.Infrastructure.IntegrationTests;

public sealed class FiscalXmlPreviewTests
{
    [Fact]
    public async Task Ecf31_preview_contains_fiscal_snapshots_and_is_explicitly_not_submittable()
    {
        var connectionString = Environment.GetEnvironmentVariable("BILLINGRD_TEST_DB")
            ?? throw new InvalidOperationException("BILLINGRD_TEST_DB is required.");

        await using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:BillingDatabase", connectionString);
                builder.UseEnvironment("Testing");
            });

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<BillingDbContext>();
            await db.Database.EnsureDeletedAsync();
            await db.Database.MigrateAsync();
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });

        Assert.Equal(HttpStatusCode.Created,
            (await client.PostAsJsonAsync("/api/auth/register", new { email = "xml@example.test", password = "StrongPass123!" })).StatusCode);

        var business = await client.PostAsJsonAsync("/api/businesses/", new { name = "XML Fiscal" });
        var businessJson = await business.Content.ReadFromJsonAsync<JsonElement>();
        var branchId = businessJson.GetProperty("branchId").GetGuid();

        Assert.Equal(HttpStatusCode.OK,
            (await client.PutAsJsonAsync("/api/fiscal-profile/", new
            {
                rnc = "101654321",
                legalName = "XML Fiscal SRL",
                tradeName = "XML Fiscal",
                address = "Calle XML 1"
            })).StatusCode);

        var customer = await client.PostAsJsonAsync("/api/customers/", new
        {
            name = "Cliente XML SRL",
            taxId = "101123456",
            fiscalAddress = "Calle Cliente 2"
        });
        var customerId = (await customer.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var product = await client.PostAsJsonAsync("/api/products/", new
        {
            name = "Servicio de integración",
            sku = "SERV-XML",
            salePrice = 100m,
            itbisCategory = "Standard",
            kind = "Service",
            tracksInventory = false
        });
        var productId = (await product.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        using var saleRequest = new HttpRequestMessage(HttpMethod.Post, "/api/sales/");
        saleRequest.Headers.Add("Idempotency-Key", "xml-preview-sale");
        saleRequest.Content = JsonContent.Create(new
        {
            branchId,
            customerId,
            items = new[] { new { productId, quantity = 1m } },
            payments = new[] { new { method = "Card", amount = 118m, reference = "XML-CARD" } }
        });

        var saleResponse = await client.SendAsync(saleRequest);
        Assert.Equal(HttpStatusCode.Created, saleResponse.StatusCode);
        var saleJson = await saleResponse.Content.ReadFromJsonAsync<JsonElement>();
        var invoiceId = saleJson.GetProperty("invoice").GetProperty("id").GetGuid();

        var draftResponse = await client.PostAsJsonAsync(
            $"/api/electronic-invoicing/drafts/invoice/{invoiceId}",
            new { type = "CreditFiscalInvoice31" });
        Assert.Equal(HttpStatusCode.Created, draftResponse.StatusCode);
        var draftId = (await draftResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();

        var previewResponse = await client.PostAsJsonAsync(
            $"/api/electronic-invoicing/drafts/{draftId}/xml-preview",
            new { eNcf = "E310000000001", sequenceExpiration = "2027-12-31" });

        Assert.Equal(HttpStatusCode.OK, previewResponse.StatusCode);
        var preview = await previewResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(preview.GetProperty("previewOnly").GetBoolean());
        Assert.False(preview.GetProperty("digitallySigned").GetBoolean());
        Assert.False(preview.GetProperty("submittableToDgii").GetBoolean());
        Assert.Equal("official-xsd-valid", preview.GetProperty("schemaValidation").GetString());
        Assert.Equal("6f2909a93d84919518d2ae3c77fead4b35c3e8c95996b8af67b0040c2e2be298", preview.GetProperty("schemaSha256").GetString());
        Assert.True(preview.GetProperty("officialSchemaNormalizedInMemory").GetBoolean());

        var xml = XDocument.Parse(preview.GetProperty("xml").GetString()!);
        Assert.Equal("ECF", xml.Root!.Name.LocalName);
        Assert.Equal("31", xml.Descendants("TipoeCF").Single().Value);
        Assert.Equal("E310000000001", xml.Descendants("eNCF").Single().Value);
        Assert.Equal("101654321", xml.Descendants("RNCEmisor").Single().Value);
        Assert.Equal("101123456", xml.Descendants("RNCComprador").Single().Value);
        Assert.Equal("1", xml.Descendants("IndicadorFacturacion").Single().Value);
        Assert.Equal("2", xml.Descendants("IndicadorBienoServicio").Single().Value);
        Assert.Equal("3", xml.Descendants("FormaPago").Single().Value);
        Assert.Equal("100.00", xml.Descendants("MontoGravadoI1").Single().Value);
        Assert.Equal("18.00", xml.Descendants("TotalITBIS").Single().Value);
        Assert.Equal("118.00", xml.Descendants("MontoTotal").Single().Value);
        Assert.Single(xml.Descendants("FechaHoraFirma"));
        Assert.Single(xml.Descendants(XName.Get("UnsignedSignaturePlaceholder", "urn:billingrd:unsigned-preview")));

        var directValidation = DgiiEcfSchemaValidator.Validate(
            BillingRD.Domain.ElectronicInvoicing.EcfType.CreditFiscalInvoice31,
            preview.GetProperty("xml").GetString()!);
        Assert.True(directValidation.IsValid);
        Assert.Empty(directValidation.Errors);
    }
    [Fact]
    public void Validator_rejects_xml_that_violates_the_pinned_official_schema()
    {
        const string invalidXml = "<ECF><Encabezado><Version>1.0</Version></Encabezado></ECF>";

        var validation = DgiiEcfSchemaValidator.Validate(
            BillingRD.Domain.ElectronicInvoicing.EcfType.ConsumerInvoice32,
            invalidXml);

        Assert.False(validation.IsValid);
        Assert.NotEmpty(validation.Errors);
        Assert.Equal(
            "151676df03c61335b4ac1eca97f99395488d57ea92793b49353602871e651f3c",
            validation.SchemaSha256);
        Assert.False(validation.OfficialSchemaNormalizedInMemory);
    }

}
