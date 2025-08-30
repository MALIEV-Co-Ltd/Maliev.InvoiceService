using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Api.Models
{
    public class UpdateInvoiceFileRequest
    {
        [Required]
        public int Id { get; set; }
        [Required]
        public int InvoiceId { get; set; }
        [Required]
        [StringLength(50)]
        public string Bucket { get; set; } = string.Empty;
        [Required]
        public string ObjectName { get; set; } = string.Empty;
    }
}
