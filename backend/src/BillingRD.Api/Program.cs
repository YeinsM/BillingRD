using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using BillingRD.Api.Endpoints;
using BillingRD.Api.Security;
using BillingRD.Application.Abstractions;
using BillingRD.Infrastructure;
using BillingRD.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = ".BillingRD.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);

        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddSingleton<IPasswordHasher<string>, PasswordHasher<string>>();

builder.Services.AddRateLimiter(rateLimiterOptions =>
{
    rateLimiterOptions.AddPolicy("auth", httpContext =>
    {
        var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
});

builder.Services.AddScoped<CurrentBusinessContext>();
builder.Services.AddScoped<ICurrentBusinessContext>(services =>
    services.GetRequiredService<CurrentBusinessContext>());

var connectionString = builder.Configuration.GetConnectionString("BillingDatabase")
    ?? throw new InvalidOperationException("Connection string 'BillingDatabase' is required.");

builder.Services.AddInfrastructure(connectionString);

var app = builder.Build();

app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<BusinessContextMiddleware>();
app.UseAuthorization();

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

app.MapAuthEndpoints();
app.MapBusinessEndpoints();
app.MapProductEndpoints();
app.MapCustomerEndpoints();
app.MapSaleEndpoints();

app.Run();

public partial class Program;
