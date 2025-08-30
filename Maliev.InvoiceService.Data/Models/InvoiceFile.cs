#nullable enable
using System;
using System.Collections.Generic;

namespace Maliev.InvoiceService.Data.Models
{
    /// <summary>
    /// Represents an invoice file.
    /// </summary>
    public partial class InvoiceFile
    {
        /// <summary>
        /// Gets or sets the unique identifier of the invoice file.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the invoice identifier associated with this file.
        /// </summary>
        public int InvoiceId { get; set; }
        /// <summary>
        /// Gets or sets the bucket where the invoice file is stored.
        /// </summary>
        public required string Bucket { get; set; }
        /// <summary>
        /// Gets or sets the object name (key) of the invoice file within the bucket.
        /// </summary>
        public required string ObjectName { get; set; }
        /// <summary>
        /// Gets or sets the creation date of the invoice file.
        /// </summary>
        public DateTime? CreatedDate { get; set; }
        /// <summary>
        /// Gets or sets the last modification date of the invoice file.
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the navigation property to the associated Invoice.
        /// </summary>
        public virtual Invoice? Invoice { get; set; }
    }
}