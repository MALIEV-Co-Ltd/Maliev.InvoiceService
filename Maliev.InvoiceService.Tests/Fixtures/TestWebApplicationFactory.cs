using Maliev.InvoiceService.Api.Services.External;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Tests.Mocks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maliev.InvoiceService.Tests.Fixtures;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly TestDatabaseFixture _databaseFixture;

    public TestWebApplicationFactory(TestDatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureTestServices(services =>
        {
            // Remove existing DbContext registration
            services.RemoveAll<DbContextOptions<InvoiceDbContext>>();
            services.RemoveAll<InvoiceDbContext>();

            // Add test database context
            services.AddDbContext<InvoiceDbContext>(options =>
            {
                options.UseNpgsql(_databaseFixture.ConnectionString);
            });

            // Replace authentication with test auth handler
            services.AddAuthentication("TestAuth")
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("TestAuth", options => { });

            // Configure test services
            services.Configure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = "TestAuth";
                options.DefaultChallengeScheme = "TestAuth";
            });

            // Add authorization policies for testing
            services.AddAuthorization(options =>
            {
                options.AddPolicy("Customer", policy => policy.RequireRole("Customer"));
                options.AddPolicy("Employee", policy => policy.RequireRole("Employee", "Manager", "Admin"));
                options.AddPolicy("Manager", policy => policy.RequireRole("Manager", "Admin"));
                options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
                options.AddPolicy("EmployeeOrHigher", policy => policy.RequireRole("Employee", "Manager", "Admin"));
            });

            // Replace Redis distributed cache with in-memory cache for testing
            services.RemoveAll<IDistributedCache>();
            services.AddDistributedMemoryCache();

            // Replace external service clients with mocks
            services.RemoveAll<ICurrencyServiceClient>();
            services.AddSingleton<ICurrencyServiceClient, MockCurrencyServiceClient>();

            services.RemoveAll<IQuotationServiceClient>();
            services.AddSingleton<IQuotationServiceClient, MockQuotationServiceClient>();
        });
    }
}
