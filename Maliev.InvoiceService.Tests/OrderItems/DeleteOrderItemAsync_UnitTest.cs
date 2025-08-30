using Maliev.InvoiceService.Api.Services;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Threading.Tasks;

namespace Maliev.InvoiceService.Tests.OrderItems
{
    public class DeleteOrderItemAsync_UnitTest
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
        public async Task DeleteOrderItemAsync_ShouldReturnTrue_WhenOrderItemExists()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new OrderItemService(context);

            var orderItem = new OrderItem { Id = 1, InvoiceId = 1, Description = "Test Item", Quantity = 1, UnitPrice = 10.00m, CreatedDate = System.DateTime.UtcNow, ModifiedDate = System.DateTime.UtcNow };
            await context.OrderItems.AddAsync(orderItem);
            await context.SaveChangesAsync();

            // Act
            var result = await service.DeleteOrderItemAsync(1);

            // Assert
            Assert.True(result);
            Assert.Null(await context.OrderItems.FindAsync(1));
        }

        [Fact]
        public async Task DeleteOrderItemAsync_ShouldReturnFalse_WhenOrderItemDoesNotExist()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new OrderItemService(context);

            // Act
            var result = await service.DeleteOrderItemAsync(99);

            // Assert
            Assert.False(result);
        }
    }
}
