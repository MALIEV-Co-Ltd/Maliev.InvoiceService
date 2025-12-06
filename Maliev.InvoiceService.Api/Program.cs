using Maliev.InvoiceService.Api.Middleware;
using Maliev.InvoiceService.Api.Models.Common;
using Maliev.InvoiceService.Api.Services.External;
using Maliev.InvoiceService.Api.Services.HealthChecks;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Data.Interceptors;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// --- Secrets & Configuration ---
builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

// --- Infrastructure & Observability ---
builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
builder.AddServiceMeters("invoices"); // Register service meters for OpenTelemetry business metrics

// Database Context with ServiceDefaults + custom interceptors
builder.AddPostgresDbContext<InvoiceDbContext>(
    connectionStringName: "InvoiceDbContext",
    configureOptions: options =>
    {
        // Add custom interceptors for audit logging and metrics
        options.AddInterceptors(
            new AuditLogInterceptor(),
            new DatabaseMetricsInterceptor()
        );
    });

builder.AddRedisDistributedCache(instanceName: "InvoiceService:"); // Redis with in-memory fallback
builder.AddMassTransitWithRabbitMq(); // RabbitMQ message bus (non-blocking startup)

// --- API Configuration ---
builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

// JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
builder.AddJwtAuthentication();

// Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
if (!builder.Environment.IsProduction())
{
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddOpenApi("v1", options =>
    {
        options.AddDocumentTransformer((document, context, cancellationToken) =>
        {
            document.Info.Title = "MALIEV Invoice Service API";
            document.Info.Version = "v1";
            document.Info.Description = "Invoice lifecycle management service. Handles invoice creation from quotations, draft editing, finalization with sequential numbering, payment recording, invoice splitting for partial billing, cancellation, CSV/JSON export, and audit trail tracking.";
            return Task.CompletedTask;
        });
    });
}

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
// Services
builder.Services.AddScoped<Maliev.InvoiceService.Api.Services.IInvoiceService, Maliev.InvoiceService.Api.Services.InvoiceService>();

// Background Services
builder.Services.AddHostedService<Maliev.InvoiceService.Api.Services.BackgroundServices.AuditArchivalService>();

// External Service Options
builder.Services.Configure<CurrencyServiceOptions>(builder.Configuration.GetSection("ExternalServices:Currency"));
builder.Services.Configure<QuotationServiceOptions>(builder.Configuration.GetSection("ExternalServices:Quotation"));

// External Service Clients with Polly v8 Resilience
builder.Services.AddHttpClient<ICurrencyServiceClient, CurrencyServiceClient>()
    .AddStandardResilienceHandler();

builder.Services.AddHttpClient<IQuotationServiceClient, QuotationServiceClient>()
    .AddStandardResilienceHandler();

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

// Run database migrations on startup (skip in Testing environment)
if (!app.Environment.IsEnvironment("Testing"))
{
    try
    {
        await app.MigrateDatabaseAsync<InvoiceDbContext>();
    }
    catch (Exception ex)
    {
        Log.MigrationFailed(logger, ex);
        // Don't throw - allow app to start for debugging
    }
}

// Middleware Pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// Map endpoints after middleware
app.MapControllers();

// Map Aspire default endpoints (/health, /alive, /metrics)
app.MapDefaultEndpoints(servicePrefix: "invoices");

// Map OpenAPI and Scalar documentation (dev/staging only)
app.MapApiDocumentation(servicePrefix: "invoices");

Log.ServiceStarted(logger);
await app.RunAsync();

/// <summary>
/// Main program class for the application
/// </summary>
public partial class Program
{
    internal static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "InvoiceService started successfully")]
        public static partial void ServiceStarted(ILogger logger);

        [LoggerMessage(Level = LogLevel.Error, Message = "Database migration failed - application may not function correctly")]
        public static partial void MigrationFailed(ILogger logger, Exception exception);
    }
}

