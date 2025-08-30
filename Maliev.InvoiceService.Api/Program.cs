using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Load secrets from mounted volume in GKE
var secretsPath = "/mnt/secrets";
if (Directory.Exists(secretsPath))
{
    builder.Configuration.AddKeyPerFile(directoryPath: secretsPath, optional: true);
}

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(option =>
{
    option.SwaggerDoc("v1", new OpenApiInfo { Title = "Invoice Service API", Version = "v1" });
    option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Please enter a valid token",
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        BearerFormat = "JWT",
        Scheme = "Bearer"
    });
    option.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type=ReferenceType.SecurityScheme,
                    Id="Bearer"
                }
            },
            new string[]{}
        }
    });
});

// Configure InvoiceContext DbContext
if (Environment.GetEnvironmentVariable("TESTING") != "true")
{
    builder.Services.AddDbContext<InvoiceContext>(options =>
    {
        options.UseSqlServer(builder.Configuration.GetConnectionString("InvoiceServiceDbContext"));
    });
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Register Invoice Services
builder.Services.AddScoped<IInvoiceService, InvoiceService>();
builder.Services.AddScoped<IInvoiceFileService, InvoiceFileService>();
builder.Services.AddScoped<IOrderItemService, OrderItemService>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(
        policy =>
        {
            policy.WithOrigins(
                "http://*.maliev.com",
                "https://*.maliev.com")
            .SetIsOriginAllowedToAllowWildcardSubdomains()
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});

// JWT Bearer authentication configuration
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtSecurityKey"]!))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Invoice Service API V1");
    c.RoutePrefix = "invoices/swagger";
});

// Secure Swagger UI
app.UseWhen(context => context.Request.Path.StartsWithSegments("/invoices/swagger"), appBuilder =>
{
    appBuilder.UseAuthorization();
});

app.UseExceptionHandler("/invoices/error"); // Add ProblemDetails exception handler
app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

// Liveness probe endpoint
app.MapGet("/invoices/liveness", () => "Healthy");

// Readiness probe endpoint
app.MapGet("/invoices/readiness", () => "Healthy");

app.MapControllers();

app.Run();