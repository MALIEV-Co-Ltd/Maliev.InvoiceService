namespace Maliev.InvoiceService.Tests.Fixtures;

/// <summary>
/// Defines a test collection that shares the TestWebApplicationFactory fixture.
/// All test classes in this collection will run sequentially and share the same factory instance.
/// </summary>
[CollectionDefinition("InvoiceService Collection")]
public class InvoiceServiceCollection : ICollectionFixture<TestWebApplicationFactory>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}
