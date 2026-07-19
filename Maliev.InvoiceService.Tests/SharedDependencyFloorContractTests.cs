namespace Maliev.InvoiceService.Tests;

using System;
using System.IO;
using System.Xml.Linq;

using Xunit;

public sealed class SharedDependencyFloorContractTests
{
    private static readonly string Root = FindRoot();

    [Theory]
    [InlineData("Maliev.InvoiceService.Api/Maliev.InvoiceService.Api.csproj", "MassTransit.EntityFrameworkCore", "[8.5.10, 9.0.0)")]
    [InlineData("Maliev.InvoiceService.Api/Maliev.InvoiceService.Api.csproj", "Microsoft.EntityFrameworkCore", "10.0.10")]
    [InlineData("Maliev.InvoiceService.Api/Maliev.InvoiceService.Api.csproj", "Microsoft.EntityFrameworkCore.Relational", "10.0.10")]
    [InlineData("Maliev.InvoiceService.Infrastructure/Maliev.InvoiceService.Infrastructure.csproj", "MassTransit.EntityFrameworkCore", "[8.5.10, 9.0.0)")]
    [InlineData("Maliev.InvoiceService.Tests/Maliev.InvoiceService.Tests.csproj", "Microsoft.NET.Test.Sdk", "18.8.1")]
    [InlineData("Maliev.InvoiceService.Tests/Maliev.InvoiceService.Tests.csproj", "xunit.runner.visualstudio", "3.1.5")]
    public void DependencyFloor_IsPinned(string project, string package, string expectedVersion)
    {
        var document = XDocument.Load(Path.Combine(Root, project));
        var reference = Assert.Single(document.Descendants("PackageReference"),
            element => string.Equals((string?)element.Attribute("Include"), package, StringComparison.Ordinal));
        Assert.Equal(expectedVersion, (string?)reference.Attribute("Version"));
    }

    [Fact]
    public void CentralMicrosoftDependencyFloor_IsCurrent()
    {
        var document = XDocument.Load(Path.Combine(Root, "Directory.Build.props"));
        foreach (var reference in document.Descendants("PackageReference"))
        {
            var package = (string?)reference.Attribute("Update");
            if (package is not null && package.StartsWith("Microsoft.", StringComparison.Ordinal))
            {
                Assert.Contains((string?)reference.Attribute("Version"), new[] { "10.0.10", "18.8.1" });
            }
        }
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Maliev.InvoiceService.slnx")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Could not locate InvoiceService repository root.");
    }
}
