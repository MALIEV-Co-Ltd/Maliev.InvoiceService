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

namespace Maliev.InvoiceService.Tests.OrderItems
{
    public class UpdateOrderItemAsync_UnitTest
    {
        private readonly InvoiceContext context;
        private readonly Mock<IOrderItemService> _mockOrderItemService;
        private readonly Mock<ILogger<OrderItemsController>> _mockLogger;

        public UpdateOrderItemAsync_UnitTest()
        {
            var options = new DbContextOptionsBuilder<InvoiceContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .EnableSensitiveDataLogging()
                .Options;

            this.context = new InvoiceContext(options);
            this.context.Database.EnsureDeleted();
            this.context.Database.EnsureCreated();

            _mockOrderItemService = new Mock<IOrderItemService>();
            _mockLogger = new Mock<ILogger<OrderItemsController>>();
        }

        [Fact]
        public async Task InvalidOrderItemData_ShouldReturnBadRequest()
        {
            // Arrange
            var controller = new OrderItemsController(_mockOrderItemService.Object, _mockLogger.Object);

            // Act
            controller.ModelState.AddModelError("Description", "Description is required.");
            var actionResult = await controller.UpdateOrderItemAsync(1, 1, new UpdateOrderItemRequest
            {
                Id = 0,
                Description = null
            });

            // Assert
            Assert.IsType<BadRequestObjectResult>(actionResult);
        }

        [Fact]
        public async Task OrderItemExist_ShouldReturnNoContent()
        {
            // Arrange
            var controller = new OrderItemsController(_mockOrderItemService.Object, _mockLogger.Object);
            var seed = new OrderItem
            {
                Id = 1,
                Description = "Test description",
                Subtotal = 12345.67m,
                InvoiceId = 1,
                Quantity = 50,
                UnitPrice = 99.99m,
                CreatedDate = new DateTime(2000, 5, 6),
            };
            await this.context.OrderItems.AddAsync(seed);
            await this.context.SaveChangesAsync();

            var newData = new UpdateOrderItemRequest
            {
                Id = seed.Id,
                Description = "New description",
                InvoiceId = 2,
                Quantity = 100,
                UnitPrice = 199.99m,
            };

            _mockOrderItemService.Setup(s => s.UpdateOrderItemAsync(seed.Id, It.IsAny<UpdateOrderItemRequest>()))
                               .ReturnsAsync(true);

            // Act
            var actionResult = await controller.UpdateOrderItemAsync(seed.InvoiceId.Value, seed.Id, newData);

            // Assert
            Assert.IsType<NoContentResult>(actionResult);
        }

        [Fact]
        public async Task OrderItemNotExist_ShouldReturnNotFound()
        {
            // Arrange
            var controller = new OrderItemsController(_mockOrderItemService.Object, _mockLogger.Object);

            _mockOrderItemService.Setup(s => s.UpdateOrderItemAsync(It.IsAny<int>(), It.IsAny<UpdateOrderItemRequest>()))
                               .ReturnsAsync(false);

            // Act
            var actionResult = await controller.UpdateOrderItemAsync(1, int.MaxValue, new UpdateOrderItemRequest
            {
                Id = int.MaxValue,
                Description = "Non-existent",
                InvoiceId = 1,
                Quantity = 1,
                UnitPrice = 1.0m
            });

            // Assert
            Assert.IsType<NotFoundResult>(actionResult);
        }
    }
}
