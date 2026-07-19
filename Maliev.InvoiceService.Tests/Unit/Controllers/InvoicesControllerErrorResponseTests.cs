using System.Security.Claims;
using Maliev.InvoiceService.Api.Authorization;
using Maliev.InvoiceService.Api.Controllers;
using Maliev.InvoiceService.Application.Models.Invoices;
using Maliev.InvoiceService.Application.Services;
using Maliev.InvoiceService.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Maliev.InvoiceService.Tests.Unit.Controllers;

public sealed class InvoicesControllerErrorResponseTests
{
    [Fact]
    public async Task CancelInvoice_UnexpectedError_DoesNotExposeExceptionDetails()
    {
        var invoiceId = Guid.NewGuid();
        var invoiceService = new Mock<IInvoiceService>();
        invoiceService
            .Setup(service => service.CancelInvoiceAsync(
                invoiceId,
                "ops",
                "duplicate test",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("storage secret path leaked"));
        var controller = CreateController(invoiceService.Object);

        var result = await controller.CancelInvoice(
            invoiceId,
            new CancelInvoiceRequest
            {
                CancelledBy = "ops",
                CancellationReason = "duplicate test"
            },
            CancellationToken.None);

        AssertInternalServerErrorWithoutDetails(result.Result);
    }

    [Fact]
    public async Task UpdateInvoice_UnexpectedError_DoesNotExposeExceptionDetails()
    {
        var invoiceId = Guid.NewGuid();
        var invoiceService = new Mock<IInvoiceService>();
        invoiceService
            .Setup(service => service.UpdateInvoiceAsync(
                invoiceId,
                It.IsAny<UpdateInvoiceRequest>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("connection string leaked"));
        var controller = CreateController(invoiceService.Object);

        var result = await controller.UpdateInvoice(
            invoiceId,
            new UpdateInvoiceRequest(),
            CancellationToken.None);

        AssertInternalServerErrorWithoutDetails(result.Result);
    }

    [Fact]
    public async Task DeleteInvoice_UnexpectedError_DoesNotExposeExceptionDetails()
    {
        var invoiceId = Guid.NewGuid();
        var invoiceService = new Mock<IInvoiceService>();
        invoiceService
            .Setup(service => service.DeleteInvoiceAsync(invoiceId, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("tenant secret leaked"));
        var controller = CreateController(invoiceService.Object);

        var result = await controller.DeleteInvoice(invoiceId, CancellationToken.None);

        AssertInternalServerErrorWithoutDetails(result);
    }

    [Fact]
    public async Task RegisterPdfFileReference_UnexpectedError_DoesNotExposeExceptionDetails()
    {
        var invoiceId = Guid.NewGuid();
        var invoiceService = new Mock<IInvoiceService>();
        invoiceService
            .Setup(service => service.RegisterPdfFileReferenceAsync(
                invoiceId,
                "files/invoice.pdf",
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidDataException("bucket secret leaked"));
        var controller = CreateController(invoiceService.Object);

        var result = await controller.RegisterPdfFileReference(
            invoiceId,
            new RegisterPdfFileReferenceRequest { PdfFileReference = "files/invoice.pdf" },
            CancellationToken.None);

        AssertInternalServerErrorWithoutDetails(result);
    }

    private static InvoicesController CreateController(IInvoiceService invoiceService)
    {
        var dbContext = new InvoiceDbContext(new DbContextOptionsBuilder<InvoiceDbContext>()
            .UseNpgsql("Host=localhost;Database=invoice_access_guard_unused")
            .Options);
        var controller = new InvoicesController(
            invoiceService,
            new InvoiceAccessGuard(dbContext),
            NullLogger<InvoicesController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim("permissions", "*"),
                        new Claim(ClaimTypes.Role, InvoicePredefinedRoles.Admin),
                        new Claim("user_id", "test-admin")
                    ],
                    authenticationType: "Test"))
            }
        };

        return controller;
    }

    private static void AssertInternalServerErrorWithoutDetails(IActionResult? result)
    {
        var objectResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        Assert.NotNull(objectResult.Value);

        var properties = objectResult.Value
            .GetType()
            .GetProperties()
            .Select(property => property.Name)
            .ToList();
        Assert.Contains("message", properties, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("details", properties, StringComparer.OrdinalIgnoreCase);
    }
}
