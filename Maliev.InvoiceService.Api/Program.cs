using Asp.Versioning;
using FluentValidation;
using Maliev.InvoiceService.Api.Middleware;
using Maliev.InvoiceService.Api.Models.Common;
using Maliev.InvoiceService.Api.Services.External;
using Maliev.InvoiceService.Api.Services.HealthChecks;
using Maliev.InvoiceService.Api.Services.Consumers;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Data.Interceptors;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using Scalar.AspNetCore;
using Serilog;
using System.Runtime.CompilerServices;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Serilog Configuration (Console JSON only)
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

try
{
    Log.Information("Starting Invoice Management Service");

    // Secrets from Google Secret Manager
    var secretsPath = "/mnt/secrets";
    if (Directory.Exists(secretsPath))
    {
        builder.Configuration.AddKeyPerFile(directoryPath: secretsPath, optional: true);
        Log.Information("Loaded secrets from {SecretsPath}", secretsPath);
    }

    // Database Context with Interceptors
    builder.Services.AddDbContext<InvoiceDbContext>((serviceProvider, options) =>
    {
        var connectionString = builder.Configuration.GetConnectionString("InvoiceDbContext")
            ?? throw new InvalidOperationException("Connection string 'InvoiceDbContext' not found");

        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(30);
            // Faster retry for development - fail fast if DB is not available
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: builder.Environment.IsDevelopment() ? 1 : 3,
                maxRetryDelay: TimeSpan.FromSeconds(builder.Environment.IsDevelopment() ? 2 : 5),
                errorCodesToAdd: null
            );
        });

        options.AddInterceptors(
            new AuditLogInterceptor(),
            new DatabaseMetricsInterceptor()
        );

        if (builder.Environment.IsDevelopment())
        {
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
    });

    // Memory Cache (simple configuration without SizeLimit)
    builder.Services.AddMemoryCache();

    // Redis Distributed Cache with fallback to in-memory
    var redisConfiguration = builder.Configuration.GetSection("Redis:ConnectionString").Value;
    if (!string.IsNullOrEmpty(redisConfiguration))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConfiguration;
            options.InstanceName = "InvoiceService_";
        });
        Log.Information("Configured Redis distributed cache at {RedisConfiguration}", redisConfiguration);
    }
    else
    {
        builder.Services.AddDistributedMemoryCache();
        Log.Warning("Redis not configured, using in-memory distributed cache");
    }

    // Services
    builder.Services.AddScoped<Maliev.InvoiceService.Api.Services.IInvoiceService, Maliev.InvoiceService.Api.Services.InvoiceService>();

    // Background Services
    builder.Services.AddHostedService<Maliev.InvoiceService.Api.Services.BackgroundServices.AuditArchivalService>();

    // FluentValidation
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // External Service Options
    builder.Services.Configure<CurrencyServiceOptions>(builder.Configuration.GetSection("ExternalServices:Currency"));
    builder.Services.Configure<QuotationServiceOptions>(builder.Configuration.GetSection("ExternalServices:Quotation"));

    // External Service Clients with Polly v8 Resilience
    builder.Services.AddHttpClient<ICurrencyServiceClient, CurrencyServiceClient>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CurrencyServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
    })
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    });

    builder.Services.AddHttpClient<IQuotationServiceClient, QuotationServiceClient>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<QuotationServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
    })
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    });

    // Payment Service Client with Polly resilience (T173)
    builder.Services.Configure<PaymentServiceOptions>(builder.Configuration.GetSection("ExternalServices:PaymentService"));
    builder.Services.AddHttpClient<IPaymentServiceClient, PaymentServiceClient>((serviceProvider, client) =>
    {
        var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<PaymentServiceOptions>>().Value;
        client.BaseAddress = new Uri(options.BaseUrl);
        client.Timeout = TimeSpan.FromSeconds(options.TimeoutInSeconds);
    })
    .AddStandardResilienceHandler(options =>
    {
        options.Retry.MaxRetryAttempts = 3;
        options.Retry.Delay = TimeSpan.FromMilliseconds(500);
        options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(20);
        options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(10);
        options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
    });

    // MassTransit with RabbitMQ (T172) - Optional for development
    var rabbitMqEnabled = builder.Configuration.GetValue<bool>("RabbitMQ:Enabled", true);
    if (rabbitMqEnabled)
    {
        Log.Information("Configuring MassTransit with RabbitMQ");
        builder.Services.AddMassTransit(x =>
        {
            // Register consumers
            x.AddConsumer<PaymentSucceededConsumer>();

            x.UsingRabbitMq((context, cfg) =>
            {
                var rabbitMqHost = builder.Configuration["RabbitMQ:Host"] ?? "localhost";
                var rabbitMqPort = builder.Configuration.GetValue<ushort>("RabbitMQ:Port", 5672);
                var rabbitMqUser = builder.Configuration["RabbitMQ:Username"] ?? "guest";
                var rabbitMqPassword = builder.Configuration["RabbitMQ:Password"] ?? "guest";

                cfg.Host(rabbitMqHost, rabbitMqPort, "/", h =>
                {
                    h.Username(rabbitMqUser);
                    h.Password(rabbitMqPassword);
                });

                // Configure consumer for PaymentSucceededEvent
                cfg.ReceiveEndpoint("invoice-service-payment-succeeded", e =>
                {
                    e.ConfigureConsumer<PaymentSucceededConsumer>(context);

                    // Bind to maliev.payments exchange with payment.succeeded routing key
                    e.Bind("maliev.payments", s =>
                    {
                        s.RoutingKey = "payment.succeeded";
                        s.ExchangeType = "topic";
                    });

                    // Retry configuration
                    e.UseMessageRetry(r => r.Intervals(
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(5),
                        TimeSpan.FromSeconds(10)));
                });
            });
        });
    }
    else
    {
        Log.Warning("RabbitMQ is disabled - payment events will not be consumed");
        // Add MassTransit with in-memory transport for development
        builder.Services.AddMassTransit(x =>
        {
            x.AddConsumer<PaymentSucceededConsumer>();
            x.UsingInMemory((context, cfg) =>
            {
                cfg.ConfigureEndpoints(context);
            });
        });
    }

    // Custom Health Checks
    builder.Services.AddSingleton<DatabaseHealthCheck>();
    builder.Services.AddSingleton<RedisHealthCheck>();

    // Controllers
    builder.Services.AddControllers();

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    });

    // OpenAPI with Swashbuckle and Scalar
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Invoice Management Service API",
            Description = "API for managing invoices, payments, and audit trails",
            Version = "v1"
        });

        // Include XML comments
        var xmlFilename = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    // JWT Authentication
    if (!builder.Environment.IsEnvironment("Testing"))
    {
        var jwtPublicKeyBase64 = builder.Configuration["JwtSettings:PublicKeyBase64"]
            ?? throw new InvalidOperationException("JWT public key not configured");

        // Double base64 decode per MALIEV standard
        var jwtPublicKeyBytes = Convert.FromBase64String(jwtPublicKeyBase64);
        var jwtPublicKeyPem = Encoding.UTF8.GetString(jwtPublicKeyBytes);

        builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = "maliev-auth-service",
                    ValidAudience = "maliev-services",
                    IssuerSigningKey = new RsaSecurityKey(
                        System.Security.Cryptography.RSA.Create())
                    {
                        KeyId = "maliev-rsa-key"
                    }
                };

                // TODO: Import RSA public key from PEM format
                // For now, this is a placeholder
                Log.Warning("JWT authentication configured with placeholder RSA key");
            });

        // Authorization Policies
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
            options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "Manager", "Admin"));
            options.AddPolicy("Manager", policy => policy.RequireRole("Manager", "Admin"));
            options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
            options.AddPolicy("EmployeeOrHigher", policy => policy.RequireRole("Employee", "Manager", "Admin"));
        });
    }

    // Health Checks with custom implementations
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "readiness", "db" })
        .AddCheck<RedisHealthCheck>("redis", tags: new[] { "readiness", "cache" });

    // Global Rate Limiting (100 req/min per user/IP)
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var userId = context.User?.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
            return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: userId,
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                });
        });

        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    // Force metrics initialization
    RuntimeHelpers.RunClassConstructor(typeof(Maliev.InvoiceService.Api.Services.InvoiceMetrics).TypeHandle);

    // Add service defaults for .NET Aspire
    builder.AddServiceDefaults();

    var app = builder.Build();

    // EXACT middleware pipeline order per CLAUDE.md
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();
    app.UseSerilogRequestLogging();

    app.UseHttpsRedirection();
    app.UseHttpMetrics();
    app.UseRateLimiter();

    if (!app.Environment.IsEnvironment("Testing"))
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    // Swagger and Scalar (development only) - mapped after middleware pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger(c =>
        {
            c.RouteTemplate = "invoices/swagger/{documentName}/swagger.json";
        });
        app.MapScalarApiReference(options =>
        {
            options
                .WithTitle("Invoice Management Service API")
                .WithTheme(ScalarTheme.Default)
                .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
                .WithEndpointPrefix("/invoices/scalar/{documentName}")
                .WithOpenApiRoutePattern("/invoices/swagger/{documentName}/swagger.json");
        });
    }

    // Health Checks
    app.MapGet("/invoices/liveness", () => Results.Ok(new { status = "Healthy", timestamp = DateTime.UtcNow }))
        .WithName("Liveness")
        .WithTags("Health")
        .AllowAnonymous();

    app.MapHealthChecks("/invoices/readiness", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = healthCheck => healthCheck.Tags.Contains("readiness"),
        ResponseWriter = HealthChecks.UI.Client.UIResponseWriter.WriteHealthCheckUIResponse
    }).AllowAnonymous();

    // Prometheus Metrics
    app.MapMetrics("/invoices/metrics");

    app.MapControllers();

    Log.Information("Invoice Management Service started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program class accessible for testing
/// <summary>
/// The main entry point for the Invoice Management Service application.
/// </summary>
public partial class Program { }
