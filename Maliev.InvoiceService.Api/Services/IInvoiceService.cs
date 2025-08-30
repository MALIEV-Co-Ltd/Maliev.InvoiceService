using Maliev.InvoiceService.Api.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Maliev.InvoiceService.Api.Services
{
    public interface IInvoiceService
    {
        Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request);
        Task<bool> DeleteInvoiceAsync(int id);
        Task<PaginatedResponse<InvoiceDto>> GetAllInvoicesAsync(InvoicePaginationRequest request);
        Task<InvoiceDto?> GetInvoiceAsync(int id);
        Task<InvoiceDto?> GetInvoiceByNumberAsync(string number);
        
        Task<bool> UpdateInvoiceAsync(int id, UpdateInvoiceRequest request);
    }
}
