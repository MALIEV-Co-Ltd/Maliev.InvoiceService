namespace Maliev.InvoiceService.Tests.Infrastructure;

public sealed class SensitiveLoggingSourceTests
{
    [Fact]
    public void PdfStorageReferences_AreNotIncludedInLogTemplates()
    {
        var repositoryRoot = FindRepositoryRoot();
        var controllerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "Maliev.InvoiceService.Api",
            "Controllers",
            "InvoicesController.cs"));
        var consumerSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "Maliev.InvoiceService.Infrastructure",
            "Consumers",
            "PdfGenerationCompletedEventConsumer.cs"));
        var serviceSource = File.ReadAllText(Path.Combine(
            repositoryRoot,
            "Maliev.InvoiceService.Infrastructure",
            "Services",
            "InvoiceService.cs"));

        Assert.DoesNotContain("{PdfFileReference}", controllerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("{StorageUrl}", consumerSource, StringComparison.Ordinal);
        Assert.DoesNotContain("{PdfFileReference}", serviceSource, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var currentDirectory = new DirectoryInfo(AppContext.BaseDirectory);
        while (currentDirectory is not null)
        {
            if (File.Exists(Path.Combine(currentDirectory.FullName, "Maliev.InvoiceService.slnx")))
            {
                return currentDirectory.FullName;
            }

            currentDirectory = currentDirectory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the InvoiceService repository root.");
    }
}
