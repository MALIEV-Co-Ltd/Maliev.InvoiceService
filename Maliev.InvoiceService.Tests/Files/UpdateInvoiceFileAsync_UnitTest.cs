using Maliev.InvoiceService.Api.Services;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Models;
using Maliev.InvoiceService.Api.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Threading.Tasks;

namespace Maliev.InvoiceService.Tests.Files
{
    public class UpdateInvoiceFileAsync_UnitTest
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
        public async Task UpdateInvoiceFileAsync_ShouldUpdateInvoiceFile_WhenInvoiceFileExists()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new InvoiceFileService(context);

            var invoiceFile = new InvoiceFile { Id = 1, InvoiceId = 1, Bucket = "oldBucket", ObjectName = "oldObject", CreatedDate = System.DateTime.UtcNow, ModifiedDate = System.DateTime.UtcNow };
            await context.InvoiceFiles.AddAsync(invoiceFile);
            await context.SaveChangesAsync();

            var request = new UpdateInvoiceFileRequest { InvoiceId = 1, Bucket = "newBucket", ObjectName = "newObject" };

            // Act
            var result = await service.UpdateInvoiceFileAsync(1, request);

            // Assert
            Assert.True(result);
            var updatedInvoiceFile = await context.InvoiceFiles.FindAsync(1);
            Assert.NotNull(updatedInvoiceFile);
            Assert.Equal("newBucket", updatedInvoiceFile.Bucket);
            Assert.Equal("newObject", updatedInvoiceFile.ObjectName);
        }

        [Fact]
        public async Task UpdateInvoiceFileAsync_ShouldReturnFalse_WhenInvoiceFileDoesNotExist()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new InvoiceFileService(context);

            var request = new UpdateInvoiceFileRequest { InvoiceId = 1, Bucket = "newBucket", ObjectName = "newObject" };

            // Act
            var result = await service.UpdateInvoiceFileAsync(99, request);

            // Assert
            Assert.False(result);
        }
    }
}
