using Maliev.InvoiceService.Tests.Fixtures;

namespace Maliev.InvoiceService.Tests.Integration;

/// <summary>
/// Base class for integration tests providing common utilities and cleanup helpers.
/// </summary>
[Collection("InvoiceService Collection")]
public abstract class BaseIntegrationTest : IClassFixture<TestWebApplicationFactory>, IAsyncLifetime
{
    protected readonly TestWebApplicationFactory Factory;
    protected readonly HttpClient Client;

    protected BaseIntegrationTest(TestWebApplicationFactory factory)
    {
        Factory = factory;
        Client = Factory.CreateAuthenticatedClient("test-admin", new[] { "admin" });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        // Clean database after all tests in this class complete
        await Factory.CleanDatabaseAsync();
    }

    /// <summary>
    /// Cleans the database to ensure test isolation.
    /// Call this at the start of each test method to ensure a clean state.
    /// </summary>
    protected async Task CleanDatabaseAsync()
    {
        await Factory.CleanDatabaseAsync();
    }
}
