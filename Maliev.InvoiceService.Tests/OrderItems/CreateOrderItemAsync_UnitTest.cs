using Maliev.InvoiceService.Api.Services;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Models;
using Maliev.InvoiceService.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Threading.Tasks;

namespace Maliev.InvoiceService.Tests.OrderItems
{
    public class CreateOrderItemAsync_UnitTest
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
        public async Task CreateOrderItemAsync_ShouldCreateNewOrderItem()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new OrderItemService(context);

            var request = new CreateOrderItemRequest
            {
                InvoiceId = 1,
                Description = "Test Item",
                Quantity = 10,
                UnitPrice = 5.00m
            };

            // Act
            var result = await service.CreateOrderItemAsync(request);

            // Assert
            Assert.NotNull(result);
            Assert.True(result.Id > 0);
            Assert.Equal(request.Description, result.Description);
            Assert.Equal(request.Quantity, result.Quantity);
            Assert.Equal(request.UnitPrice, result.UnitPrice);
        }
    }
}
