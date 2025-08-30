using System;
using System.ComponentModel.DataAnnotations;

namespace Maliev.InvoiceService.Api.Models
{
    public class InvoiceFileDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string? Bucket { get; set; }
        public string? ObjectName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }
}
