using BillingRD.Application.Abstractions;
using BillingRD.Infrastructure;
using BillingRD.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();

// Until authentication resolves a trusted membership, business-scoped queries fail closed.
builder.Services.AddScoped<ICurrentBusinessContext, UnresolvedCurrentBusinessContext>();

var connectionString = builder.Configuration.GetConnectionString("BillingDatabase")
    ?? throw new InvalidOperationException("Connection string 'BillingDatabase' is required.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

app.UseExceptionHandler();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "BillingRD.Api"
}));

app.MapGet("/ready", async (BillingDbContext dbContext, CancellationToken cancellationToken) =>
{
    var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
    return canConnect
        ? Results.Ok(new { status = "ready", database = "connected" })
        : Results.Problem("Database connection is unavailable.", statusCode: StatusCodes.Status503ServiceUnavailable);
});

app.Run();

public partial class Program;

internal sealed class UnresolvedCurrentBusinessContext : ICurrentBusinessContext
{
    public Guid? BusinessId => null;
}
