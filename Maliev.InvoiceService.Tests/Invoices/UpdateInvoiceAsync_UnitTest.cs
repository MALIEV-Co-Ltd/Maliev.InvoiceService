// <copyright file="UpdateInvoiceAsync_UnitTest.cs" company="Maliev Company Limited">
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
    using System.Collections.Generic;

    /// <summary>
    /// UnitTest.
    /// </summary>
    public class UpdateInvoiceAsync_UnitTest
    {
        /// <summary>
        /// The context.
        /// </summary>
        private readonly InvoiceContext context;
        private readonly Mock<IInvoiceService> _mockInvoiceService;
        private readonly Mock<ILogger<InvoicesController>> _mockLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateInvoiceAsync_UnitTest"/> class.
        /// </summary>
        public UpdateInvoiceAsync_UnitTest()
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
        /// Invalid order file data, should return bad request object result.
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
            controller.ModelState.AddModelError("Number", "Number is required.");
            var actionResult = await controller.UpdateInvoiceAsync(1, new UpdateInvoiceRequest
            {
                Id = 1,
                Number = null,
                CustomerId = 1,
                IsPaid = false
            });

            // Assert
            Assert.IsType<BadRequestObjectResult>(actionResult);
        }

        /// <summary>
        /// Invoice not exist, should return not found.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task InvoiceNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);

            _mockInvoiceService.Setup(s => s.UpdateInvoiceAsync(It.IsAny<int>(), It.IsAny<UpdateInvoiceRequest>()))
                               .ReturnsAsync(false); // Mock service to return false for not found

            // Act
            var actionResult = await controller.UpdateInvoiceAsync(int.MaxValue, new UpdateInvoiceRequest
            {
                Id = int.MaxValue,
                Number = "non-existent",
                CustomerId = 1,
                IsPaid = false
            });

            // Assert
            Assert.IsType<NotFoundResult>(actionResult);
        }

        /// <summary>
        /// Valid order file data, should return no content.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task ValidInvoiceData_ShouldReturnNoContent()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var seed = new Invoice
            {
                Id = 1,
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
                CreatedDate = new DateTime(2000, 5, 6),
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
                TaxIdentification = "1254ofaweo12",
                ReceiptId = 1234,
                WithholdingTax = 300.0m,
                InternalComment = "test internal comment",
                InvoiceFiles = new HashSet<InvoiceFile>(),
                OrderItems = new HashSet<OrderItem>()
            };
            await this.context.Invoices.AddAsync(seed);
            await this.context.SaveChangesAsync();

            var newData = new UpdateInvoiceRequest
            {
                Id = seed.Id,
                CustomerId = 6666,
                BillingAddressBuilding = "new billing building",
                BillingAddressCity = "new billing city",
                BillingAddressCompany = "new billing company",
                BillingAddressCountry = "new billing country",
                BillingAddressLine1 = "new billing line 1",
                BillingAddressLine2 = "new billing line 2",
                BillingAddressPostalCode = "new billing postal code",
                BillingAddressRecipient = "new billing recipient",
                BillingAddressState = "new billing state",
                Comment = "new test comment",
                Currency = "new ABC",
                Fob = "new test fob",
                IsPaid = true,
                Number = "new 123-456-789",
                PaymentDate = DateTime.UtcNow,
                PurchaseOrderNumber = "new 987-654-321",
                Requisitioner = "new test requisitioner",
                SalesPerson = "new test sales person",
                ShippedVia = "new test shipped via",
                ShippingAddressBuilding = "new shipping building",
                ShippingAddressCity = "new shipping city",
                ShippingAddressCompany = "new shipping company",
                ShippingAddressCountry = "new shipping country",
                ShippingAddressLine1 = "new shipping line 1",
                ShippingAddressLine2 = "new shipping line 2",
                ShippingAddressPostalCode = "new shipping postal code",
                ShippingAddressRecipient = "new shipping recipient",
                ShippingAddressState = "new shipping state",
                Outstanding = 32100.12m,
                Subtotal = 45600.54m,
                Terms = "new test terms",
                Total = 987600.54m,
                Vat = 15900.74m,
                CommercialRegistration = "new comm. registration",
                TaxIdentification = "uavnaerv1234",
                ReceiptId = 321,
                WithholdingTax = 200m,
                InternalComment = "new internal comment",
            };

            _mockInvoiceService.Setup(s => s.UpdateInvoiceAsync(seed.Id, It.IsAny<UpdateInvoiceRequest>()))
                               .ReturnsAsync(true);

            // Act
            var actionResult = await controller.UpdateInvoiceAsync(seed.Id, newData);

            // Assert
            Assert.IsType<NoContentResult>(actionResult);
        }

        /// <summary>
        /// No invoice data, should return bad request.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task NoInvoiceData_ShouldReturnBadRequest()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);

            // Act
            controller.ModelState.AddModelError("request", "Request cannot be null.");
            var actionResult = await controller.UpdateInvoiceAsync(1, null);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actionResult);
        }
    }
}