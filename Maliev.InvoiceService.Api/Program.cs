using Maliev.InvoiceService.Api.Models.Common;
using Maliev.InvoiceService.Api.Services.External;
using Maliev.InvoiceService.Api.Services;
using Maliev.Aspire.ServiceDefaults;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Data.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

// Initialize bootstrap logging
using var loggerFactory = LoggerFactory.Create(logBuilder => logBuilder.AddConsole());
var bootstrapLogger = loggerFactory.CreateLogger("Program");

try
{
    bootstrapLogger.LogInformation("Starting Invoice Service host");

    var builder = WebApplication.CreateBuilder(args);

    // --- Secrets & Configuration ---
    builder.AddGoogleSecretManagerVolume(); // Load secrets from /mnt/secrets if available

    // --- Infrastructure & Observability ---
    builder.AddServiceDefaults(); // OpenTelemetry, health checks, resilience
    builder.AddStandardMiddleware(options =>
    {
        options.EnableRequestLogging = true;
    });
    builder.AddServiceMeters("invoices-meter"); // Register service meters for OpenTelemetry business metrics

    builder.Services.AddHttpContextAccessor();

    // Database Context with ServiceDefaults + custom interceptors
    builder.AddPostgresDbContext<InvoiceDbContext>(
        connectionName: "InvoiceDbContext",
        configureOptions: (sp, options) =>
        {
            options.AddInterceptors(
                new AuditLogInterceptor(sp.GetService<IHttpContextAccessor>()),
                new DatabaseMetricsInterceptor()
            );
        });

    builder.AddRedisDistributedCache(instanceName: "invoice:"); // Redis with in-memory fallback
    builder.AddMassTransitWithRabbitMq(x =>
    {
        x.AddConsumer<Maliev.InvoiceService.Api.Services.Consumers.FileDeletedEventConsumer>();
        x.AddConsumer<Maliev.InvoiceService.Api.Services.Consumers.PaymentCompletedEventConsumer>();
        x.AddConsumer<Maliev.InvoiceService.Api.Services.Consumers.OrderPaidEventConsumer>();
        x.AddConsumer<Maliev.InvoiceService.Api.Services.Consumers.PdfGenerationCompletedEventConsumer>();
    }); // RabbitMQ message bus (non-blocking startup)

    // --- API Configuration ---
    builder.AddDefaultCors(); // CORS from CORS:AllowedOrigins config
    builder.AddDefaultApiVersioning(); // API versioning with URL segment reader

    // JWT Authentication (tests override via PostConfigureAll with dynamic RSA keys)
    builder.AddJwtAuthentication();

    // Register permissions/roles on startup
    builder.AddIAMServiceClient("invoice");
    builder.Services.AddIAMRegistration<InvoiceIAMRegistrationService>("invoice");

    // Register claims transformation for legacy role mapping
    builder.Services.AddTransient<Microsoft.AspNetCore.Authentication.IClaimsTransformation, Maliev.InvoiceService.Api.Authorization.IAMClaimsTransformation>();

    // Authorization with Permission Policy Provider
    builder.Services.AddPermissionAuthorization();

    // Add OpenAPI (must be in Program.cs for XML comments to work via source generator)
    if (!builder.Environment.IsProduction())
    {
        builder.AddStandardOpenApi(
            title: "MALIEV Invoice Service API",
            description: "Invoice lifecycle management service. Handles invoice creation from quotations, draft editing, finalization with sequential numbering, payment recording, invoice splitting for partial billing, cancellation, CSV/JSON export, and audit trail tracking.");
    }

    builder.Services.AddControllers();
    builder.Services.AddMemoryCache();
    // Services
    builder.Services.AddScoped<Maliev.InvoiceService.Api.Services.IInvoiceService, Maliev.InvoiceService.Api.Services.InvoiceService>();

    // Background Services
    builder.Services.AddHostedService<Maliev.InvoiceService.Api.Services.BackgroundServices.AuditArchivalService>();

    // External Service Clients with Polly v8 Resilience
    builder.AddServiceClient<ICurrencyServiceClient, CurrencyServiceClient>("CurrencyService");
    builder.AddServiceClient<IQuotationServiceClient, QuotationServiceClient>("QuotationService");
    builder.AddServiceClient<IPaymentServiceClient, PaymentServiceClient>("PaymentService");

    var app = builder.Build();
    var logger = app.Services.GetRequiredService<ILogger<Maliev.InvoiceService.Api.Program>>();

    // --- Database Migrations ---
    await app.MigrateDatabaseAsync<InvoiceDbContext>();

    // --- Middleware Pipeline ---
    app.UseStandardMiddleware();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHttpsRedirection();
    }
    app.UseCors();

    app.UseAuthentication();
    app.UseAuthorization();

    // Map endpoints after middleware
    app.MapControllers();

    // Map Aspire default endpoints (/health, /alive, /metrics)
    app.MapDefaultEndpoints(servicePrefix: "invoice");

    // Map OpenAPI and Scalar documentation (dev/staging only)
    app.MapApiDocumentation(servicePrefix: "invoice");

    logger.LogInformation("InvoiceService started successfully");
    await app.RunAsync();
}
catch (Exception ex)
{
    bootstrapLogger.LogCritical(ex, "Invoice Service host terminated unexpectedly during startup");
    throw;
}
finally
{
    loggerFactory.Dispose();
}

namespace Maliev.InvoiceService.Api
{
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
}
