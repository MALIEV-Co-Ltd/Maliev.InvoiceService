// <copyright file="CreateInvoiceAsync_UnitTest.cs" company="Maliev Company Limited">
// Copyright (c) Maliev Company Limited. All rights reserved.
// </copyright>

namespace Maliev.InvoiceService.Tests.Invoices
{
    using System;
    using System.Threading.Tasks;
    using Maliev.InvoiceService.Api.Controllers;
    using Maliev.InvoiceService.Api.Models;
    using Maliev.InvoiceService.Api.Services;
    using Maliev.InvoiceService.Data.Data;
    using Maliev.InvoiceService.Data.Models;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;
    using Moq;
    using Xunit;
    using System.Collections.Generic; // Added for HashSet

    /// <summary>
    /// UnitTest.
    /// </summary>
    public class CreateInvoiceAsync_UnitTest
    {
        /// <summary>
        /// The context.
        /// </summary>
        private readonly InvoiceContext context;
        private readonly Mock<IInvoiceService> _mockInvoiceService;
        private readonly Mock<ILogger<InvoicesController>> _mockLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateInvoiceAsync_UnitTest"/> class.
        /// </summary>
        public CreateInvoiceAsync_UnitTest()
        {
            var options = new DbContextOptionsBuilder<InvoiceContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Use unique name for each test
                .EnableSensitiveDataLogging()
                .Options;

            this.context = new InvoiceContext(options);
            this.context.Database.EnsureDeleted();
            this.context.Database.EnsureCreated();

            _mockInvoiceService = new Mock<IInvoiceService>();
            _mockLogger = new Mock<ILogger<InvoicesController>>();
        }

        /// <summary>
        /// Valid Invoice data, should return bad request with error content.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task InvalidInvoiceData_ShouldReturnBadRequestObjectResult()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);

            // Act
            // Simulate invalid model state by passing null or an invalid request
            controller.ModelState.AddModelError("Number", "Number is required.");
            var actionResult = await controller.CreateInvoiceAsync(new CreateInvoiceRequest
            {
                Number = null,
                CustomerId = 0,
                IsPaid = false
            }); // Pass an empty request to trigger validation

            // Assert
            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        }

        /// <summary>
        /// Valid Invoice data, should return created at route.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task ValidInvoiceData_ShouldReturnCreatedAtRoute()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var testdata = new CreateInvoiceRequest
            {
                CustomerId = 5555,
                BillingAddressBuilding = "billing building",
                BillingAddressCity = "billing city",
                BillingAddressCompany = "billing company",
                BillingAddressCountry = "billing country",
                BillingAddressLine1 = "billing line 1",
                BillingAddressLine2 = "billing line 2",
                BillingAddressPostalCode = "billing postal code",
                BillingAddressRecipient = "billing recipient",
                BillingAddressState = "billing state",
                Comment = "test comment",
                Currency = "ABC",
                Fob = "test fob",
                IsPaid = true,
                Number = "123-456-789",
                PaymentDate = DateTime.UtcNow,
                PurchaseOrderNumber = "987-654-321",
                Requisitioner = "test requisitioner",
                SalesPerson = "test sales person",
                ShippedVia = "test shipped via",
                ShippingAddressBuilding = "shipping building",
                ShippingAddressCity = "shipping city",
                ShippingAddressCompany = "shipping company",
                ShippingAddressCountry = "shipping country",
                ShippingAddressLine1 = "shipping line 1",
                ShippingAddressLine2 = "shipping line 2",
                ShippingAddressPostalCode = "shipping postal code",
                ShippingAddressRecipient = "shipping recipient",
                ShippingAddressState = "shipping state",
                Outstanding = 321.12m,
                Subtotal = 456.54m,
                Terms = "test terms",
                Total = 9876.54m,
                Vat = 159.74m,
                CommercialRegistration = "test comm. registration",
                TaxIdentification = "12341oawev12",
                ReceiptId = 1234,
                WithholdingTax = 300.00m,
                InternalComment = "Test internal comment",
            };

            var expectedInvoiceDto = new InvoiceDto
            {
                Id = 1,
                Number = testdata.Number,
                CustomerId = testdata.CustomerId,
                Comment = testdata.Comment,
                InternalComment = testdata.InternalComment,
                SalesPerson = testdata.SalesPerson,
                Currency = testdata.Currency,
                PurchaseOrderNumber = testdata.PurchaseOrderNumber,
                Requisitioner = testdata.Requisitioner,
                ShippedVia = testdata.ShippedVia,
                Fob = testdata.Fob,
                Terms = testdata.Terms,
                BillingAddressRecipient = testdata.BillingAddressRecipient,
                BillingAddressCompany = testdata.BillingAddressCompany,
                BillingAddressBuilding = testdata.BillingAddressBuilding,
                BillingAddressLine1 = testdata.BillingAddressLine1,
                BillingAddressLine2 = testdata.BillingAddressLine2,
                BillingAddressCity = testdata.BillingAddressCity,
                BillingAddressState = testdata.BillingAddressState,
                BillingAddressPostalCode = testdata.BillingAddressPostalCode,
                BillingAddressCountry = testdata.BillingAddressCountry,
                ShippingAddressRecipient = testdata.ShippingAddressRecipient,
                ShippingAddressRecipientTelephone = testdata.ShippingAddressRecipientTelephone,
                ShippingAddressCompany = testdata.ShippingAddressCompany,
                ShippingAddressBuilding = testdata.ShippingAddressBuilding,
                ShippingAddressLine1 = testdata.ShippingAddressLine1,
                ShippingAddressLine2 = testdata.ShippingAddressLine2,
                ShippingAddressCity = testdata.ShippingAddressCity,
                ShippingAddressState = testdata.ShippingAddressState,
                ShippingAddressPostalCode = testdata.ShippingAddressPostalCode,
                ShippingAddressCountry = testdata.ShippingAddressCountry,
                CommercialRegistration = testdata.CommercialRegistration,
                TaxIdentification = testdata.TaxIdentification,
                Subtotal = testdata.Subtotal,
                Vat = testdata.Vat,
                Total = testdata.Total,
                WithholdingTax = testdata.WithholdingTax,
                Outstanding = testdata.Outstanding,
                IsPaid = testdata.IsPaid,
                ReceiptId = testdata.ReceiptId,
                PaymentDate = testdata.PaymentDate,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _mockInvoiceService.Setup(s => s.CreateInvoiceAsync(It.IsAny<CreateInvoiceRequest>()))
                               .ReturnsAsync(expectedInvoiceDto);

            // Act
            var actionResult = await controller.CreateInvoiceAsync(testdata);

            // Assert
            Assert.IsType<CreatedAtRouteResult>(actionResult.Result);
            var createdInvoiceDto = (InvoiceDto)((CreatedAtRouteResult)actionResult.Result).Value;
            Assert.NotNull(createdInvoiceDto);
            Assert.Equal(expectedInvoiceDto.Number, createdInvoiceDto.Number);
            Assert.Equal(expectedInvoiceDto.CustomerId, createdInvoiceDto.CustomerId);
            // Add more assertions for other properties if needed
        }
    }
}