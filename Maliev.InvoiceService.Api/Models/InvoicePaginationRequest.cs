namespace Maliev.InvoiceService.Api.Models
{
    public class InvoicePaginationRequest
    {
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
