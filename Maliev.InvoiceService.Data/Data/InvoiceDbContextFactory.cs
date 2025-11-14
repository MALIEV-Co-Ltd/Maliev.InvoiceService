using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Maliev.InvoiceService.Data.Data;

/// <summary>
/// Design-time factory for InvoiceDbContext.
/// Required for EF Core migrations and tooling.
/// Uses environment variable for connection string.
/// </summary>
public class InvoiceDbContextFactory : IDesignTimeDbContextFactory<InvoiceDbContext>
{
    public InvoiceDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InvoiceDbContext>();

        // Get connection string from environment variable
        var connectionString = Environment.GetEnvironmentVariable("InvoiceDbContext")
            ?? "Server=localhost;Port=5432;Database=invoice_app_db;User Id=postgres;Password=postgres;";

        optionsBuilder.UseNpgsql(connectionString, options =>
        {
            options.MigrationsAssembly("Maliev.InvoiceService.Data");
            options.CommandTimeout(30);
        });

        return new InvoiceDbContext(optionsBuilder.Options);
    }
}
