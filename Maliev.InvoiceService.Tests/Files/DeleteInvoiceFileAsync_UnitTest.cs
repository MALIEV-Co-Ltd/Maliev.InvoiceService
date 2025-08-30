// <copyright file="DeleteInvoiceFileAsync_UnitTest.cs" company="Maliev Company Limited">
// Copyright (c) Maliev Company Limited. All rights reserved.
// </copyright>

namespace Maliev.InvoiceService.Tests.Files
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
    public class DeleteInvoiceFileAsync_UnitTest
    {
        /// <summary>
        /// The context.
        /// </summary>
        private readonly InvoiceContext context;
        private readonly Mock<IInvoiceFileService> _mockInvoiceFileService;
        private readonly Mock<ILogger<FilesController>> _mockLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeleteInvoiceFileAsync_UnitTest"/> class.
        /// </summary>
        public DeleteInvoiceFileAsync_UnitTest()
        {
            var options = new DbContextOptionsBuilder<InvoiceContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()) // Use unique name for each test
                .EnableSensitiveDataLogging()
                .Options;

            this.context = new InvoiceContext(options);
            this.context.Database.EnsureDeleted();
            this.context.Database.EnsureCreated();

            _mockInvoiceFileService = new Mock<IInvoiceFileService>();
            _mockLogger = new Mock<ILogger<FilesController>>();
        }

        /// <summary>
        /// Invoice file exist, should return no content.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task InvoiceFileExist_ShouldReturnNoContent()
        {
            // Arrange
            var controller = new FilesController(_mockInvoiceFileService.Object, _mockLogger.Object);
            var seed = new InvoiceFile { Id = 1, InvoiceId = 1, Bucket = "maliev", ObjectName = "test-object" };
            await this.context.InvoiceFiles.AddAsync(seed);
            await this.context.SaveChangesAsync();

            _mockInvoiceFileService.Setup(s => s.DeleteInvoiceFileAsync(seed.Id))
                                   .ReturnsAsync(true); // Mock service to return true for successful deletion

            // Act
            var actionResult = await controller.DeleteInvoiceFileAsync(seed.Id);

            // Assert
            Assert.IsType<NoContentResult>(actionResult);
        }

        /// <summary>
        /// Invoice file not exist, should return not found.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task InvoiceFileNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var controller = new FilesController(_mockInvoiceFileService.Object, _mockLogger.Object);

            _mockInvoiceFileService.Setup(s => s.DeleteInvoiceFileAsync(It.IsAny<int>()))
                                   .ReturnsAsync(false); // Mock service to return false for not found

            // Act
            var actionResult = await controller.DeleteInvoiceFileAsync(int.MaxValue);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult);
        }
    }
}
