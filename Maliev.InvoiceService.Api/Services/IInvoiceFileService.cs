using Maliev.InvoiceService.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maliev.InvoiceService.Api.Services
{
    public interface IInvoiceFileService
    {
        Task<InvoiceFileDto> CreateInvoiceFileAsync(CreateInvoiceFileRequest request);
        
        Task<bool> DeleteInvoiceFileAsync(int invoiceId);
        Task<List<InvoiceFileDto>> GetAllInvoiceFilesAsync();
        Task<List<InvoiceFileDto>> GetInvoiceFilesAsync(int invoiceId);
        Task<InvoiceFileDto?> GetInvoiceFileAsync(int invoiceId);
        Task<bool> UpdateInvoiceFileAsync(int invoiceId, UpdateInvoiceFileRequest request);
    }
}
