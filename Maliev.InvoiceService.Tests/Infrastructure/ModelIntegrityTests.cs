using Maliev.InvoiceService.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Maliev.InvoiceService.Tests.Infrastructure;

public class ModelIntegrityTests
{
    [Fact]
    public void Model_ShouldNotHavePendingChanges()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseNpgsql("Host=localhost;Database=ModelCheck")
            .Options;

        using var context = new InvoiceDbContext(options);
        var hasChanges = context.Database.HasPendingModelChanges();

        Assert.False(hasChanges, "Run 'dotnet ef migrations add <Name> --project Maliev.InvoiceService.Data --startup-project Maliev.InvoiceService.Api'");
    }

    [Fact]
    public void Model_ShouldIncludeMassTransitOutboxEntities()
    {
        var options = new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseNpgsql("Host=localhost;Database=ModelCheck")
            .Options;

        using var context = new InvoiceDbContext(options);
        var entityNames = context.Model.GetEntityTypes()
            .Select(entity => entity.ClrType.FullName)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("MassTransit.EntityFrameworkCoreIntegration.InboxState", entityNames);
        Assert.Contains("MassTransit.EntityFrameworkCoreIntegration.OutboxMessage", entityNames);
        Assert.Contains("MassTransit.EntityFrameworkCoreIntegration.OutboxState", entityNames);
    }
}
