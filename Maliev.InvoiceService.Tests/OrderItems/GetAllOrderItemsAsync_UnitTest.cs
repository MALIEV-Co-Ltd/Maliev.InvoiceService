using Maliev.InvoiceService.Api.Services;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Maliev.InvoiceService.Tests.OrderItems
{
    public class GetAllOrderItemsAsync_UnitTest
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
        public async Task GetAllOrderItemsAsync_ShouldReturnAllOrderItems()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new OrderItemService(context);

            var orderItem1 = new OrderItem { InvoiceId = 1, Description = "Item 1", Quantity = 1, UnitPrice = 10.00m, CreatedDate = System.DateTime.UtcNow, ModifiedDate = System.DateTime.UtcNow };
            var orderItem2 = new OrderItem { InvoiceId = 1, Description = "Item 2", Quantity = 2, UnitPrice = 20.00m, CreatedDate = System.DateTime.UtcNow, ModifiedDate = System.DateTime.UtcNow };
            var orderItem3 = new OrderItem { InvoiceId = 2, Description = "Item 3", Quantity = 3, UnitPrice = 30.00m, CreatedDate = System.DateTime.UtcNow, ModifiedDate = System.DateTime.UtcNow };

            await context.OrderItems.AddRangeAsync(orderItem1, orderItem2, orderItem3);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetAllOrderItemsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, o => o.Description == "Item 1");
            Assert.Contains(result, o => o.Description == "Item 2");
            Assert.Contains(result, o => o.Description == "Item 3");
        }

        [Fact]
        public async Task GetAllOrderItemsAsync_ShouldReturnEmptyList_WhenNoOrderItemsExist()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new OrderItemService(context);

            // Act
            var result = await service.GetAllOrderItemsAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
