using Maliev.InvoiceService.Api.Services;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Threading.Tasks;

namespace Maliev.InvoiceService.Tests.Files
{
    public class GetInvoiceFileAsync_UnitTest
    {
        private InvoiceContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<InvoiceContext>()
                .UseInMemoryDatabase(databaseName: $"InvoiceFileTestDb_{System.Guid.NewGuid()}")
                .Options;
            var context = new InvoiceContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        [Fact]
        public async Task GetInvoiceFileAsync_ShouldReturnInvoiceFile_WhenInvoiceFileExists()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new InvoiceFileService(context);

            var invoiceFile = new InvoiceFile { Id = 1, InvoiceId = 1, Bucket = "bucket1", ObjectName = "object1", CreatedDate = System.DateTime.UtcNow, ModifiedDate = System.DateTime.UtcNow };
            await context.InvoiceFiles.AddAsync(invoiceFile);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetInvoiceFileAsync(1);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Id);
            Assert.Equal("object1", result.ObjectName);
        }

        [Fact]
        public async Task GetInvoiceFileAsync_ShouldReturnNull_WhenInvoiceFileDoesNotExist()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new InvoiceFileService(context);

            // Act
            var result = await service.GetInvoiceFileAsync(99);

            // Assert
            Assert.Null(result);
        }
    }
}
