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
using System;
using System.Collections.Generic;

namespace Maliev.InvoiceService.Tests.Invoices
{
    public class GetInvoiceAsync_UnitTest
    {
        private readonly InvoiceContext context;
        private readonly Mock<IInvoiceService> _mockInvoiceService;
        private readonly Mock<ILogger<InvoicesController>> _mockLogger;

        public GetInvoiceAsync_UnitTest()
        {
            var options = new DbContextOptionsBuilder<InvoiceContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            this.context = new InvoiceContext(options);
            this.context.Database.EnsureDeleted();
            this.context.Database.EnsureCreated();

            _mockInvoiceService = new Mock<IInvoiceService>();
            _mockLogger = new Mock<ILogger<InvoicesController>>();
        }

        [Fact]
        public async Task GetInvoiceAsync_ById_ShouldReturnInvoice_WhenInvoiceExists()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var seed = new Invoice { Id = 1, Number = "123-456-789", CustomerId = 1, IsPaid = false, InvoiceFiles = new HashSet<InvoiceFile>(), OrderItems = new HashSet<OrderItem>() };
            
            var expectedInvoiceDto = new InvoiceDto
            {
                Id = seed.Id,
                Number = seed.Number,
                CustomerId = seed.CustomerId,
                IsPaid = seed.IsPaid,
                Comment = seed.Comment,
                InternalComment = seed.InternalComment,
                SalesPerson = seed.SalesPerson,
                Currency = seed.Currency,
                PurchaseOrderNumber = seed.PurchaseOrderNumber,
                Requisitioner = seed.Requisitioner,
                ShippedVia = seed.ShippedVia,
                Fob = seed.Fob,
                Terms = seed.Terms,
                BillingAddressRecipient = seed.BillingAddressRecipient,
                BillingAddressCompany = seed.BillingAddressCompany,
                BillingAddressBuilding = seed.BillingAddressBuilding,
                BillingAddressLine1 = seed.BillingAddressLine1,
                BillingAddressLine2 = seed.BillingAddressLine2,
                BillingAddressCity = seed.BillingAddressCity,
                BillingAddressState = seed.BillingAddressState,
                BillingAddressPostalCode = seed.BillingAddressPostalCode,
                BillingAddressCountry = seed.BillingAddressCountry,
                ShippingAddressRecipient = seed.ShippingAddressRecipient,
                ShippingAddressRecipientTelephone = seed.ShippingAddressRecipientTelephone,
                ShippingAddressCompany = seed.ShippingAddressCompany,
                ShippingAddressBuilding = seed.ShippingAddressBuilding,
                ShippingAddressLine1 = seed.ShippingAddressLine1,
                ShippingAddressLine2 = seed.ShippingAddressLine2,
                ShippingAddressCity = seed.ShippingAddressCity,
                ShippingAddressState = seed.ShippingAddressState,
                ShippingAddressPostalCode = seed.ShippingAddressPostalCode,
                ShippingAddressCountry = seed.ShippingAddressCountry,
                CommercialRegistration = seed.CommercialRegistration,
                TaxIdentification = seed.TaxIdentification,
                Subtotal = seed.Subtotal,
                Vat = seed.Vat,
                Total = seed.Total,
                WithholdingTax = seed.WithholdingTax,
                Outstanding = seed.Outstanding,
                ReceiptId = seed.ReceiptId,
                PaymentDate = seed.PaymentDate,
                CreatedDate = seed.CreatedDate,
                ModifiedDate = seed.ModifiedDate
            };

            _mockInvoiceService.Setup(s => s.GetInvoiceAsync(seed.Id))
                               .ReturnsAsync(expectedInvoiceDto);

            // Act
            var actionResult = await controller.GetInvoiceByIdAsync(seed.Id);

            // Assert
            Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedInvoiceDto = (InvoiceDto)((OkObjectResult)actionResult.Result).Value;
            Assert.NotNull(returnedInvoiceDto);
            Assert.Equal(expectedInvoiceDto.Id, returnedInvoiceDto.Id);
            Assert.Equal(expectedInvoiceDto.Number, returnedInvoiceDto.Number);
        }

        [Fact]
        public async Task GetInvoiceAsync_ById_ShouldReturnNotFound_WhenInvoiceDoesNotExist()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);

            _mockInvoiceService.Setup(s => s.GetInvoiceAsync(It.IsAny<int>()))
                               .ReturnsAsync((InvoiceDto)null);

            // Act
            var actionResult = await controller.GetInvoiceByIdAsync(int.MaxValue);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        [Fact]
        public async Task GetInvoiceAsync_ByNumber_ShouldReturnInvoice_WhenInvoiceExists()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var seed = new Invoice { Id = 2, Number = "987-654-321", CustomerId = 1, IsPaid = true, InvoiceFiles = new HashSet<InvoiceFile>(), OrderItems = new HashSet<OrderItem>() };

            var expectedInvoiceDto = new InvoiceDto
            {
                Id = seed.Id,
                Number = seed.Number,
                CustomerId = seed.CustomerId,
                IsPaid = seed.IsPaid,
                Comment = seed.Comment,
                InternalComment = seed.InternalComment,
                SalesPerson = seed.SalesPerson,
                Currency = seed.Currency,
                PurchaseOrderNumber = seed.PurchaseOrderNumber,
                Requisitioner = seed.Requisitioner,
                ShippedVia = seed.ShippedVia,
                Fob = seed.Fob,
                Terms = seed.Terms,
                BillingAddressRecipient = seed.BillingAddressRecipient,
                BillingAddressCompany = seed.BillingAddressCompany,
                BillingAddressBuilding = seed.BillingAddressBuilding,
                BillingAddressLine1 = seed.BillingAddressLine1,
                BillingAddressLine2 = seed.BillingAddressLine2,
                BillingAddressCity = seed.BillingAddressCity,
                BillingAddressState = seed.BillingAddressState,
                BillingAddressPostalCode = seed.BillingAddressPostalCode,
                BillingAddressCountry = seed.BillingAddressCountry,
                ShippingAddressRecipient = seed.ShippingAddressRecipient,
                ShippingAddressRecipientTelephone = seed.ShippingAddressRecipientTelephone,
                ShippingAddressCompany = seed.ShippingAddressCompany,
                ShippingAddressBuilding = seed.ShippingAddressBuilding,
                ShippingAddressLine1 = seed.ShippingAddressLine1,
                ShippingAddressLine2 = seed.ShippingAddressLine2,
                ShippingAddressCity = seed.ShippingAddressCity,
                ShippingAddressState = seed.ShippingAddressState,
                ShippingAddressPostalCode = seed.ShippingAddressPostalCode,
                ShippingAddressCountry = seed.ShippingAddressCountry,
                CommercialRegistration = seed.CommercialRegistration,
                TaxIdentification = seed.TaxIdentification,
                Subtotal = seed.Subtotal,
                Vat = seed.Vat,
                Total = seed.Total,
                WithholdingTax = seed.WithholdingTax,
                Outstanding = seed.Outstanding,
                ReceiptId = seed.ReceiptId,
                PaymentDate = seed.PaymentDate,
                CreatedDate = seed.CreatedDate,
                ModifiedDate = seed.ModifiedDate
            };

            _mockInvoiceService.Setup(s => s.GetInvoiceByNumberAsync(seed.Number))
                               .ReturnsAsync(expectedInvoiceDto);

            // Act
            var actionResult = await controller.GetInvoiceByNumberAsync(seed.Number);

            // Assert
            Assert.IsType<OkObjectResult>(actionResult.Result);
            var returnedInvoiceDto = (InvoiceDto)((OkObjectResult)actionResult.Result).Value;
            Assert.NotNull(returnedInvoiceDto);
            Assert.Equal(expectedInvoiceDto.Id, returnedInvoiceDto.Id);
            Assert.Equal(expectedInvoiceDto.Number, returnedInvoiceDto.Number);
        }

        [Fact]
        public async Task GetInvoiceAsync_ByNumber_ShouldReturnNotFound_WhenInvoiceDoesNotExist()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);

            _mockInvoiceService.Setup(s => s.GetInvoiceByNumberAsync(It.IsAny<string>()))
                               .ReturnsAsync((InvoiceDto)null);

            // Act
            var actionResult = await controller.GetInvoiceByNumberAsync("NonExistentNumber");

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }
    }
}
