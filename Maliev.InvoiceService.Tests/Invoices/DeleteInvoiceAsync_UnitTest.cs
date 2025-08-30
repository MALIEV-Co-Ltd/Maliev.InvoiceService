// <copyright file="DeleteInvoiceAsync_UnitTest.cs" company="Maliev Company Limited">
// Copyright (c) Maliev Company Limited. All rights reserved.
// </copyright>

namespace Maliev.InvoiceService.Tests.Invoices
{
    using System.Threading.Tasks;
    using Maliev.InvoiceService.Api.Controllers;
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

    /// <summary>
    /// UnitTest.
    /// </summary>
    public class DeleteInvoiceAsync_UnitTest
    {
        /// <summary>
        /// The context.
        /// </summary>
        private readonly InvoiceContext context;
        private readonly Mock<IInvoiceService> _mockInvoiceService;
        private readonly Mock<ILogger<InvoicesController>> _mockLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteInvoiceAsync_UnitTest"/> class.
        /// </summary>
        public DeleteInvoiceAsync_UnitTest()
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
        /// Invoice exist, should return no content.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task InvoiceExist_ShouldReturnNoContent()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var seed = new Invoice { Id = 1, Number = "12456789", CustomerId = 1, IsPaid = false, InvoiceFiles = new HashSet<InvoiceFile>(), OrderItems = new HashSet<OrderItem>() };
            await this.context.Invoices.AddAsync(seed);
            await this.context.SaveChangesAsync();

            _mockInvoiceService.Setup(s => s.DeleteInvoiceAsync(seed.Id))
                               .ReturnsAsync(true); // Mock service to return true for successful deletion

            // Act
            var actionResult = await controller.DeleteInvoiceAsync(seed.Id);

            // Assert
            Assert.IsType<NoContentResult>(actionResult);
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

            _mockInvoiceService.Setup(s => s.DeleteInvoiceAsync(It.IsAny<int>()))
                               .ReturnsAsync(false); // Mock service to return false for not found

            // Act
            var actionResult = await controller.DeleteInvoiceAsync(int.MaxValue);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult);
        }
    }
}
