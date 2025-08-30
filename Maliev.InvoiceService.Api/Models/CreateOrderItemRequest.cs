using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Api.Models
{
    public class CreateOrderItemRequest
{
        public int? InvoiceId { get; set; }
        [Required]
        public string Description { get; set; } = string.Empty;
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
    }
}
