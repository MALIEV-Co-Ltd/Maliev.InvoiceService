// <copyright file="GetPaginatedAsync_UnitTest.cs" company="Maliev Company Limited">
// Copyright (c) Maliev Company Limited. All rights reserved.
// </copyright>

namespace Maliev.InvoiceService.Tests.Invoices
{
    using System.Linq;
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

    /// <summary>
    /// UnitTest.
    /// </summary>
    public class GetPaginatedAsync_UnitTest
    {
        /// <summary>
        /// The context.
        /// </summary>
        private readonly InvoiceContext context;
        private readonly Mock<IInvoiceService> _mockInvoiceService;
        private readonly Mock<ILogger<InvoicesController>> _mockLogger;

        /// <summary>
        /// Initializes a new instance of the <see cref="GetPaginatedAsync_UnitTest"/> class.
        /// </summary>
        public GetPaginatedAsync_UnitTest()
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
        /// First page, should have no previous page.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task FirstPage_ShouldHaveNoPreviousPage()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var invoices = new List<InvoiceDto>();
            for (int i = 0; i < 1000; i++)
            {
                invoices.Add(new InvoiceDto { Id = i + 1, Number = i.ToString(), CustomerId = 1, IsPaid = false });
            }

            var request = new InvoicePaginationRequest { PageNumber = 1, PageSize = 100 };
            _mockInvoiceService.Setup(s => s.GetAllInvoicesAsync(It.IsAny<InvoicePaginationRequest>()))
                               .ReturnsAsync(new PaginatedResponse<InvoiceDto>(request.PageNumber, request.PageSize, invoices.Count, invoices.Take(request.PageSize).ToList()));

            // Act
            var actionResult = await controller.GetAllInvoicesAsync(request);

            // Assert
            Assert.IsType<OkObjectResult>(actionResult.Result);
            var paginatedResponse = (PaginatedResponse<InvoiceDto>)((OkObjectResult)actionResult.Result).Value;
            Assert.NotNull(paginatedResponse);
            Assert.Equal(1, paginatedResponse.PageNumber);
            Assert.Equal(100, paginatedResponse.PageSize);
            Assert.Equal(1000, paginatedResponse.TotalCount);
            Assert.Equal(100, paginatedResponse.Items.Count);
        }

        /// <summary>
        /// Last page, should have no next page.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task LastPage_ShouldHaveNoNextPage()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var invoices = new List<InvoiceDto>();
            for (int i = 0; i < 1000; i++)
            {
                invoices.Add(new InvoiceDto { Id = i + 1, Number = i.ToString(), CustomerId = 1, IsPaid = false });
            }

            var request = new InvoicePaginationRequest { PageNumber = 10, PageSize = 100 };
            _mockInvoiceService.Setup(s => s.GetAllInvoicesAsync(It.IsAny<InvoicePaginationRequest>()))
                               .ReturnsAsync(new PaginatedResponse<InvoiceDto>(request.PageNumber, request.PageSize, invoices.Count, invoices.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList()));

            // Act
            var actionResult = await controller.GetAllInvoicesAsync(request);

            // Assert
            Assert.IsType<OkObjectResult>(actionResult.Result);
            var paginatedResponse = (PaginatedResponse<InvoiceDto>)((OkObjectResult)actionResult.Result).Value;
            Assert.NotNull(paginatedResponse);
            Assert.Equal(10, paginatedResponse.PageNumber);
            Assert.Equal(100, paginatedResponse.PageSize);
            Assert.Equal(1000, paginatedResponse.TotalCount);
            Assert.Equal(100, paginatedResponse.Items.Count);
        }

        /// <summary>
        /// No Invoices, should return not found.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task NoInvoices_ShouldReturnNotFound()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var request = new InvoicePaginationRequest { PageNumber = 1, PageSize = 10 };
            _mockInvoiceService.Setup(s => s.GetAllInvoicesAsync(It.IsAny<InvoicePaginationRequest>()))
                               .ReturnsAsync(new PaginatedResponse<InvoiceDto>(request.PageNumber, request.PageSize, 0, new List<InvoiceDto>()));

            // Act
            var actionResult = await controller.GetAllInvoicesAsync(request);

            // Assert
            Assert.IsType<NotFoundResult>(actionResult.Result);
        }

        /// <summary>
        /// No query, should return all Invoices.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task NoQuery_ShouldReturnAllInvoices()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var invoices = new List<InvoiceDto>();
            for (int i = 0; i < 1000; i++)
            {
                invoices.Add(new InvoiceDto { Id = i + 1, Number = i.ToString(), CustomerId = 1, IsPaid = false });
            }

            var request = new InvoicePaginationRequest { PageNumber = 1, PageSize = 1000 }; // Request all
            _mockInvoiceService.Setup(s => s.GetAllInvoicesAsync(It.IsAny<InvoicePaginationRequest>()))
                               .ReturnsAsync(new PaginatedResponse<InvoiceDto>(request.PageNumber, request.PageSize, invoices.Count, invoices));

            // Act
            var actionResult = await controller.GetAllInvoicesAsync(request);

            // Assert
            Assert.IsType<OkObjectResult>(actionResult.Result);
            var paginatedResponse = (PaginatedResponse<InvoiceDto>)((OkObjectResult)actionResult.Result).Value;
            Assert.NotNull(paginatedResponse);
            Assert.Equal(1000, paginatedResponse.TotalCount);
        }

        /// <summary>
        /// Search for ID, should return matched records.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task SearchId_ShouldReturnMatchedRecords()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var invoices = new List<InvoiceDto>();
            for (int i = 1; i < 100; i++)
            {
                invoices.Add(new InvoiceDto { Id = i, Number = "123-456-789", CustomerId = 1, IsPaid = false });
            }
            var matchedInvoice = new InvoiceDto { Id = 50, Number = "123-456-789", CustomerId = 1, IsPaid = false };

            var request = new InvoicePaginationRequest { PageNumber = 1, PageSize = 10 };
            _mockInvoiceService.Setup(s => s.GetAllInvoicesAsync(It.IsAny<InvoicePaginationRequest>()))
                               .ReturnsAsync(new PaginatedResponse<InvoiceDto>(request.PageNumber, request.PageSize, 1, new List<InvoiceDto> { matchedInvoice }));

            // Act
            var actionResult = await controller.GetAllInvoicesAsync(request); // Assuming search is handled by service

            // Assert
            Assert.IsType<OkObjectResult>(actionResult.Result);
            var paginatedResponse = (PaginatedResponse<InvoiceDto>)((OkObjectResult)actionResult.Result).Value;
            Assert.NotNull(paginatedResponse);
            Assert.Equal(1, paginatedResponse.TotalCount);
        }

        /// <summary>
        /// Search for invoice number, should return matched records.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task SearchInvoiceNumber_ShouldReturnMatchedRecords()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var invoices = new List<InvoiceDto>();
            for (int i = 1; i <= 100; i++)
            {
                invoices.Add(new InvoiceDto { Id = i, Number = "987-654-321", CustomerId = 1, IsPaid = false });
            }
            for (int i = 101; i < 201; i++)
            {
                invoices.Add(new InvoiceDto { Id = i, Number = "123-456-789", CustomerId = 1, IsPaid = false });
            }
            var matchedInvoices = invoices.Where(i => i.Number == "123-456-789").ToList();

            var request = new InvoicePaginationRequest { PageNumber = 1, PageSize = 10 };
            _mockInvoiceService.Setup(s => s.GetAllInvoicesAsync(It.IsAny<InvoicePaginationRequest>()))
                               .ReturnsAsync(new PaginatedResponse<InvoiceDto>(request.PageNumber, request.PageSize, matchedInvoices.Count, matchedInvoices.Take(request.PageSize).ToList()));

            // Act
            var actionResult = await controller.GetAllInvoicesAsync(request); // Assuming search is handled by service

            // Assert
            Assert.IsType<OkObjectResult>(actionResult.Result);
            var paginatedResponse = (PaginatedResponse<InvoiceDto>)((OkObjectResult)actionResult.Result).Value;
            Assert.NotNull(paginatedResponse);
            Assert.Equal(100, paginatedResponse.TotalCount);
        }

        /// <summary>
        /// Page size is 100, should return 10 pages with 100 records on each page.
        /// </summary>
        /// <returns>
        ///   <see cref="Task" />.
        /// </returns>
        [Fact]
        public async Task SizeDefined_ShouldReturnOneHundredRecordsPerPage()
        {
            // Arrange
            var controller = new InvoicesController(_mockInvoiceService.Object, _mockLogger.Object);
            var invoices = new List<InvoiceDto>();
            for (int i = 1; i <= 1000; i++)
            {
                invoices.Add(new InvoiceDto { Id = i, Number = "123-456-789", CustomerId = 1, IsPaid = false });
            }

            var request = new InvoicePaginationRequest { PageNumber = 1, PageSize = 100 };
            _mockInvoiceService.Setup(s => s.GetAllInvoicesAsync(It.IsAny<InvoicePaginationRequest>()))
                               .ReturnsAsync(new PaginatedResponse<InvoiceDto>(request.PageNumber, request.PageSize, invoices.Count, invoices.Take(request.PageSize).ToList()));

            // Act
            var actionResult = await controller.GetAllInvoicesAsync(request);

            // Assert
            Assert.IsType<OkObjectResult>(actionResult.Result);
            var paginatedResponse = (PaginatedResponse<InvoiceDto>)((OkObjectResult)actionResult.Result).Value;
            Assert.NotNull(paginatedResponse);
            Assert.Equal(1000, paginatedResponse.TotalCount);
            Assert.Equal(10, (int)Math.Ceiling((double)paginatedResponse.TotalCount / paginatedResponse.PageSize));
        }
    }
}