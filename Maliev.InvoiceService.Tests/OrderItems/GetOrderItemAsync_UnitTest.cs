using Maliev.InvoiceService.Api.Services;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Threading.Tasks;

namespace Maliev.InvoiceService.Tests.OrderItems
{
    public class GetOrderItemAsync_UnitTest
    {
        private InvoiceContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<InvoiceContext>()
                .UseInMemoryDatabase(databaseName: $"OrderItemTestDb_{System.Guid.NewGuid()}")
                .Options;
            var context = new InvoiceContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task GetOrderItemAsync_ShouldReturnOrderItem_WhenOrderItemExists()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new OrderItemService(context);

            var orderItem = new OrderItem { Id = 1, InvoiceId = 1, Description = "Test Item", Quantity = 1, UnitPrice = 10.00m, CreatedDate = System.DateTime.UtcNow, ModifiedDate = System.DateTime.UtcNow };
            await context.OrderItems.AddAsync(orderItem);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetOrderItemAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("Test Item", result.Description);
        }

        [Fact]
        public async Task GetOrderItemAsync_ShouldReturnNull_WhenOrderItemDoesNotExist()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new OrderItemService(context);

            // Act
            var result = await service.GetOrderItemAsync(99);

            // Assert
            Assert.Null(result);
        }
    }
}
