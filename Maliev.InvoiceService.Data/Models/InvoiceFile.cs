#nullable enable
using System;
using System.Collections.Generic;

namespace Maliev.InvoiceService.Data.Models
{
    public partial class InvoiceFile
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public required string Bucket { get; set; }
        public required string ObjectName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public virtual Invoice? Invoice { get; set; }
    }
}