using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Api.Models
{
    public class UpdateOrderItemRequest
    {
        [Required]
        public int Id { get; set; }
        public int? InvoiceId { get; set; }
        [Required]
        public string Description { get; set; } = string.Empty;
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
    }
}
