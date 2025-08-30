#nullable enable
using System;
using System.Collections.Generic;

namespace Maliev.InvoiceService.Data.Models
{
    public partial class Invoice
    {
        public Invoice()
        {
            InvoiceFiles = new HashSet<InvoiceFile>();
            OrderItems = new HashSet<OrderItem>();
        }

        public int Id { get; set; }
        public required string Number { get; set; }
        public int CustomerId { get; set; }
        public string? Comment { get; set; }
        public string? InternalComment { get; set; }
        public string? SalesPerson { get; set; }
        public string? Currency { get; set; }
        public string? PurchaseOrderNumber { get; set; }
        public string? Requisitioner { get; set; }
        public string? ShippedVia { get; set; }
        public string? Fob { get; set; }
        public string? Terms { get; set; }
        public string? BillingAddressRecipient { get; set; }
        public string? BillingAddressCompany { get; set; }
        public string? BillingAddressBuilding { get; set; }
        public string? BillingAddressLine1 { get; set; }
        public string? BillingAddressLine2 { get; set; }
        public string? BillingAddressCity { get; set; }
        public string? BillingAddressState { get; set; }
        public string? BillingAddressPostalCode { get; set; }
        public string? BillingAddressCountry { get; set; }
        public string? ShippingAddressRecipient { get; set; }
        public string? ShippingAddressRecipientTelephone { get; set; }
        public string? ShippingAddressCompany { get; set; }
        public string? ShippingAddressBuilding { get; set; }
        public string? ShippingAddressLine1 { get; set; }
        public string? ShippingAddressLine2 { get; set; }
        public string? ShippingAddressCity { get; set; }
        public string? ShippingAddressState { get; set; }
        public string? ShippingAddressPostalCode { get; set; }
        public string? ShippingAddressCountry { get; set; }
        public string? CommercialRegistration { get; set; }
        public string? TaxIdentification { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Vat { get; set; }
        public decimal? Total { get; set; }
        public decimal? WithholdingTax { get; set; }
        public decimal? Outstanding { get; set; }
        public bool IsPaid { get; set; }
        public int? ReceiptId { get; set; }
        public DateTime? PaymentDate { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? ModifiedDate { get; set; }

        public virtual ICollection<InvoiceFile> InvoiceFiles { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
    }
}