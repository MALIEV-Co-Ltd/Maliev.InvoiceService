// <copyright file="CreateInvoiceFileEntryAsync_UnitTest.cs" company="Maliev Company Limited">
// Copyright (c) Maliev Company Limited. All rights reserved.
// </copyright>

namespace Maliev.InvoiceService.Tests.Files
{
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
    using System.Linq;

    /// <summary>
    /// UnitTest.
    /// </summary>
    public class CreateInvoiceFileEntryAsync_UnitTest
    {
        /// <summary>
        /// The context.
        /// </summary>
        private readonly InvoiceContext context;
        private readonly Mock<IInvoiceFileService> _mockInvoiceFileService;
        private readonly Mock<ILogger<FilesController>> _mockLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateInvoiceFileEntryAsync_UnitTest" /> class.
        /// </summary>
        public CreateInvoiceFileEntryAsync_UnitTest()
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
        /// Invalids the bucket argument should return bad request object result.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task InvalidBucketArgument_ShouldReturnBadRequestObjectResult()
        {
            // Arrange
            var controller = new FilesController(_mockInvoiceFileService.Object, _mockLogger.Object);
            var invoice = new Invoice { Id = 1, Number = "12456789", CustomerId = 1, IsPaid = false, InvoiceFiles = new HashSet<InvoiceFile>(), OrderItems = new HashSet<OrderItem>() };
            await this.context.Invoices.AddAsync(invoice);
            await this.context.SaveChangesAsync();

            var request = new CreateInvoiceFileRequest
            {
                InvoiceId = invoice.Id,
                Bucket = null, // Invalid argument
                ObjectName = "test-object"
            };

            _mockInvoiceFileService.Setup(s => s.CreateInvoiceFileAsync(It.IsAny<CreateInvoiceFileRequest>()))
                                   .ReturnsAsync((InvoiceFileDto)null); // Mock service to return null for invalid input

            // Act
            controller.ModelState.AddModelError("Bucket", "Bucket cannot be empty."); // Simulate model state error
            var actionResult = await controller.CreateInvoiceFileAsync(request.InvoiceId, request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        }

        /// <summary>
        /// Invalids the object name argument should return bad request object result.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task InvalidObjectNameArgument_ShouldReturnBadRequestObjectResult()
        {
            // Arrange
            var controller = new FilesController(_mockInvoiceFileService.Object, _mockLogger.Object);
            var invoice = new Invoice { Id = 1, Number = "12456789", CustomerId = 1, IsPaid = false, InvoiceFiles = new HashSet<InvoiceFile>(), OrderItems = new HashSet<OrderItem>() };
            await this.context.Invoices.AddAsync(invoice);
            await this.context.SaveChangesAsync();

            var request = new CreateInvoiceFileRequest
            {
                InvoiceId = invoice.Id,
                Bucket = "test-bucket",
                ObjectName = null // Invalid argument
            };

            _mockInvoiceFileService.Setup(s => s.CreateInvoiceFileAsync(It.IsAny<CreateInvoiceFileRequest>()))
                                   .ReturnsAsync((InvoiceFileDto)null); // Mock service to return null for invalid input

            // Act
            controller.ModelState.AddModelError("ObjectName", "ObjectName cannot be empty."); // Simulate model state error
            var actionResult = await controller.CreateInvoiceFileAsync(request.InvoiceId, request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        }

        /// <summary>
        /// Invoice exist, should return created at route.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task InvoiceExist_ShouldReturnCreatedAtRoute()
        {
            // Arrange
            var controller = new FilesController(_mockInvoiceFileService.Object, _mockLogger.Object);
            var invoice = new Invoice { Id = 1, Number = "12456789", CustomerId = 1, IsPaid = false, InvoiceFiles = new HashSet<InvoiceFile>(), OrderItems = new HashSet<OrderItem>() };
            await this.context.Invoices.AddAsync(invoice);
            await this.context.SaveChangesAsync();

            var request = new CreateInvoiceFileRequest
            {
                InvoiceId = invoice.Id,
                Bucket = "test-bucket",
                ObjectName = "test-object"
            };

            var expectedInvoiceFileDto = new InvoiceFileDto
            {
                Id = 1,
                InvoiceId = request.InvoiceId,
                Bucket = request.Bucket,
                ObjectName = request.ObjectName,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            _mockInvoiceFileService.Setup(s => s.CreateInvoiceFileAsync(It.IsAny<CreateInvoiceFileRequest>()))
                                   .ReturnsAsync(expectedInvoiceFileDto);

            // Act
            var actionResult = await controller.CreateInvoiceFileAsync(request.InvoiceId, request);

            // Assert
            Assert.IsType<CreatedAtRouteResult>(actionResult.Result);
            var createdInvoiceFileDto = (InvoiceFileDto)((CreatedAtRouteResult)actionResult.Result).Value;
            Assert.NotNull(createdInvoiceFileDto);
            Assert.Equal(expectedInvoiceFileDto.Bucket, createdInvoiceFileDto.Bucket);
            Assert.Equal(expectedInvoiceFileDto.ObjectName, createdInvoiceFileDto.ObjectName);
            Assert.Equal(expectedInvoiceFileDto.InvoiceId, createdInvoiceFileDto.InvoiceId);
        }

        /// <summary>
        /// Invoices the not exist should return not found object result.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task InvoiceNotExist_ShouldReturnNotFoundObjectResult()
        {
            // Arrange
            var controller = new FilesController(_mockInvoiceFileService.Object, _mockLogger.Object);

            var request = new CreateInvoiceFileRequest
            {
                InvoiceId = int.MaxValue, // Invoice does not exist
                Bucket = "test-bucket",
                ObjectName = "test-object"
            };

            _mockInvoiceFileService.Setup(s => s.CreateInvoiceFileAsync(It.IsAny<CreateInvoiceFileRequest>()))
                                   .ReturnsAsync((InvoiceFileDto)null); // Mock service to return null

            // Act
            var actionResult = await controller.CreateInvoiceFileAsync(request.InvoiceId, request);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }
    }
}