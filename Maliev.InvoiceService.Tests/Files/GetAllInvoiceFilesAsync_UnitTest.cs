using Maliev.InvoiceService.Api.Services;
using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Maliev.InvoiceService.Tests.Files
{
    public class GetAllInvoiceFilesAsync_UnitTest
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
        public async Task GetAllInvoiceFilesAsync_ShouldReturnAllInvoiceFiles()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new InvoiceFileService(context);

            var invoiceFile1 = new InvoiceFile { InvoiceId = 1, Bucket = "bucket1", ObjectName = "object1", CreatedDate = System.DateTime.UtcNow, ModifiedDate = System.DateTime.UtcNow };
            var invoiceFile2 = new InvoiceFile { InvoiceId = 2, Bucket = "bucket2", ObjectName = "object2", CreatedDate = System.DateTime.UtcNow, ModifiedDate = System.DateTime.UtcNow };
            var invoiceFile3 = new InvoiceFile { InvoiceId = 3, Bucket = "bucket3", ObjectName = "object3", CreatedDate = System.DateTime.UtcNow, ModifiedDate = System.DateTime.UtcNow };

            await context.InvoiceFiles.AddRangeAsync(invoiceFile1, invoiceFile2, invoiceFile3);
            await context.SaveChangesAsync();

            // Act
            var result = await service.GetAllInvoiceFilesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Equal(3, result.Count);
            Assert.Contains(result, f => f.ObjectName == "object1");
            Assert.Contains(result, f => f.ObjectName == "object2");
            Assert.Contains(result, f => f.ObjectName == "object3");
        }

        [Fact]
        public async Task GetAllInvoiceFilesAsync_ShouldReturnEmptyList_WhenNoInvoiceFilesExist()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new InvoiceFileService(context);

            // Act
            var result = await service.GetAllInvoiceFilesAsync();

            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }
    }
}
