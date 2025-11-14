using Maliev.InvoiceService.Data.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Maliev.InvoiceService.Tests.Fixtures;

public class TestDatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container;

    public TestDatabaseFixture()
    {
        _container = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("invoice_service_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        // Apply migrations
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        using var context = new InvoiceDbContext(options);
        await context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        await _container.DisposeAsync();
    }

    public async Task ClearDatabaseAsync()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        using var context = new InvoiceDbContext(options);

        // Use TRUNCATE to bypass prevent_finalized_deletion trigger
        // TRUNCATE is faster and cleaner for test cleanup
        await context.Database.ExecuteSqlRawAsync(@"
            TRUNCATE TABLE audit_logs CASCADE;
            TRUNCATE TABLE invoice_payments CASCADE;
            TRUNCATE TABLE invoice_lines CASCADE;
            TRUNCATE TABLE invoices CASCADE;
            TRUNCATE TABLE payments CASCADE;
        ");

        // Clear connection pool to prevent connection issues
        using var connection = new NpgsqlConnection(ConnectionString);
        NpgsqlConnection.ClearPool(connection);
    }
}
