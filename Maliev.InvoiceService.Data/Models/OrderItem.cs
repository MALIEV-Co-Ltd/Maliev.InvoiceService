#nullable enable
using System;
using System.Collections.Generic;

namespace Maliev.InvoiceService.Data.Models
{
    /// <summary>
    /// Represents an order item within an invoice.
    /// </summary>
    public partial class OrderItem
    {
        /// <summary>
        /// Gets or sets the unique identifier of the order item.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the invoice identifier associated with this order item.
        /// </summary>
        public int? InvoiceId { get; set; }
        /// <summary>
        /// Gets or sets the description of the order item.
        /// </summary>
        public string? Description { get; set; }
        /// <summary>
        /// Gets or sets the quantity of the order item.
        /// </summary>
        public int? Quantity { get; set; }
        /// <summary>
        /// Gets or sets the unit price of the order item.
        /// </summary>
        public decimal? UnitPrice { get; set; }
        /// <summary>
        /// Gets or sets the subtotal of the order item.
        /// </summary>
        public decimal? Subtotal { get; set; }
        /// <summary>
        /// Gets or sets the creation date of the order item.
        /// </summary>
        public DateTime? CreatedDate { get; set; }
        /// <summary>
        /// Gets or sets the last modification date of the order item.
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated Invoice.
        /// </summary>
        public virtual Invoice? Invoice { get; set; }
    }
}