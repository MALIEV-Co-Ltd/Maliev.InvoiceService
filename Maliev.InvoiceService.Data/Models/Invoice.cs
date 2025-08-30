#nullable enable
using System;
using System.Collections.Generic;

namespace Maliev.InvoiceService.Data.Models
{
    /// <summary>
    /// Represents an invoice.
    /// </summary>
    public partial class Invoice
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Invoice"/> class.
        /// </summary>
        public Invoice()
        {
            InvoiceFiles = new HashSet<InvoiceFile>();
            OrderItems = new HashSet<OrderItem>();
        }

        /// <summary>
        /// Gets or sets the unique identifier of the invoice.
        /// </summary>
        public int Id { get; set; }
        /// <summary>
        /// Gets or sets the invoice number.
        /// </summary>
        public required string Number { get; set; }
        /// <summary>
        /// Gets or sets the customer identifier.
        /// </summary>
        public int CustomerId { get; set; }
        /// <summary>
        /// Gets or sets the comment.
        /// </summary>
        public string? Comment { get; set; }
        /// <summary>
        /// Gets or sets the internal comment.
        /// </summary>
        public string? InternalComment { get; set; }
        /// <summary>
        /// Gets or sets the sales person.
        /// </summary>
        public string? SalesPerson { get; set; }
        /// <summary>
        /// Gets or sets the currency.
        /// </summary>
        public string? Currency { get; set; }
        /// <summary>
        /// Gets or sets the purchase order number.
        /// </summary>
        public string? PurchaseOrderNumber { get; set; }
        /// <summary>
        /// Gets or sets the requisitioner.
        /// </summary>
        public string? Requisitioner { get; set; }
        /// <summary>
        /// Gets or sets the shipped via.
        /// </summary>
        public string? ShippedVia { get; set; }
        /// <summary>
        /// Gets or sets the FOB (Free On Board) terms.
        /// </summary>
        public string? Fob { get; set; }
        /// <summary>
        /// Gets or sets the payment terms.
        /// </summary>
        public string? Terms { get; set; }
        /// <summary>
        /// Gets or sets the billing address recipient.
        /// </summary>
        public string? BillingAddressRecipient { get; set; }
        /// <summary>
        /// Gets or sets the billing address company.
        /// </summary>
        public string? BillingAddressCompany { get; set; }
        /// <summary>
        /// Gets or sets the billing address building.
        /// </summary>
        public string? BillingAddressBuilding { get; set; }
        /// <summary>
        /// Gets or sets the billing address line 1.
        /// </summary>
        public string? BillingAddressLine1 { get; set; }
        /// <summary>
        /// Gets or sets the billing address line 2.
        /// </summary>
        public string? BillingAddressLine2 { get; set; }
        /// <summary>
        /// Gets or sets the billing address city.
        /// </summary>
        public string? BillingAddressCity { get; set; }
        /// <summary>
        /// Gets or sets the billing address state.
        /// </summary>
        public string? BillingAddressState { get; set; }
        /// <summary>
        /// Gets or sets the billing address postal code.
        /// </summary>
        public string? BillingAddressPostalCode { get; set; }
        /// <summary>
        /// Gets or sets the billing address country.
        /// </summary>
        public string? BillingAddressCountry { get; set; }
        /// <summary>
        /// Gets or sets the shipping address recipient.
        /// </summary>
        public string? ShippingAddressRecipient { get; set; }
        /// <summary>
        /// Gets or sets the shipping address recipient telephone.
        /// </summary>
        public string? ShippingAddressRecipientTelephone { get; set; }
        /// <summary>
        /// Gets or sets the shipping address company.
        /// </summary>
        public string? ShippingAddressCompany { get; set; }
        /// <summary>
        /// Gets or sets the shipping address building.
        /// </summary>
        public string? ShippingAddressBuilding { get; set; }
        /// <summary>
        /// Gets or sets the shipping address line 1.
        /// </summary>
        public string? ShippingAddressLine1 { get; set; }
        /// <summary>
        /// Gets or sets the shipping address line 2.
        /// </summary>
        public string? ShippingAddressLine2 { get; set; }
        /// <summary>
        /// Gets or sets the shipping address city.
        /// </summary>
        public string? ShippingAddressCity { get; set; }
        /// <summary>
        /// Gets or sets the shipping address state.
        /// </summary>
        public string? ShippingAddressState { get; set; }
        /// <summary>
        /// Gets or sets the shipping address postal code.
        /// </summary>
        public string? ShippingAddressPostalCode { get; set; }
        /// <summary>
        /// Gets or sets the shipping address country.
        /// </summary>
        public string? ShippingAddressCountry { get; set; }
        /// <summary>
        /// Gets or sets the commercial registration.
        /// </summary>
        public string? CommercialRegistration { get; set; }
        /// <summary>
        /// Gets or sets the tax identification.
        /// </summary>
        public string? TaxIdentification { get; set; }
        /// <summary>
        /// Gets or sets the subtotal of the invoice.
        /// </summary>
        public decimal? Subtotal { get; set; }
        /// <summary>
        /// Gets or sets the VAT (Value Added Tax) amount.
        /// </summary>
        public decimal? Vat { get; set; }
        /// <summary>
        /// Gets or sets the total amount of the invoice.
        /// </summary>
        public decimal? Total { get; set; }
        /// <summary>
        /// Gets or sets the withholding tax amount.
        /// </summary>
        public decimal? WithholdingTax { get; set; }
        /// <summary>
        /// Gets or sets the outstanding amount.
        /// </summary>
        public decimal? Outstanding { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether the invoice is paid.
        /// </summary>
        public bool IsPaid { get; set; }
        /// <summary>
        /// Gets or sets the receipt identifier.
        /// </summary>
        public int? ReceiptId { get; set; }
        /// <summary>
        /// Gets or sets the payment date.
        /// </summary>
        public DateTime? PaymentDate { get; set; }
        /// <summary>
        /// Gets or sets the creation date of the invoice.
        /// </summary>
        public DateTime? CreatedDate { get; set; }
        /// <summary>
        /// Gets or sets the last modification date of the invoice.
        /// </summary>
        public DateTime? ModifiedDate { get; set; }

        /// <summary>
        /// Gets or sets the collection of invoice files associated with this invoice.
        /// </summary>
        public virtual ICollection<InvoiceFile> InvoiceFiles { get; set; }
        /// <summary>
        /// Gets or sets the collection of order items associated with this invoice.
        /// </summary>
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}