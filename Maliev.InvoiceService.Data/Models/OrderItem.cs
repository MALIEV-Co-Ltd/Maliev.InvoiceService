#nullable enable
using System;
using System.Collections.Generic;

namespace Maliev.InvoiceService.Data.Models
{
    public partial class OrderItem
    {
        public int Id { get; set; }
        public int? InvoiceId { get; set; }
        public string? Description { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? Subtotal { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public virtual Invoice? Invoice { get; set; }
    }
}