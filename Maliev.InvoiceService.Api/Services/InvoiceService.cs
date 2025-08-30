using Maliev.InvoiceService.Data.Data;
using Maliev.InvoiceService.Data.Models;
using Maliev.InvoiceService.Api.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Maliev.InvoiceService.Api.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly InvoiceContext _context;

        public InvoiceService(InvoiceContext context)
        {
            _context = context;
        }

        public async Task<InvoiceDto> CreateInvoiceAsync(CreateInvoiceRequest request)
        {
            var invoice = new Invoice
            {
                Number = request.Number,
                CustomerId = request.CustomerId,
                Comment = request.Comment,
                InternalComment = request.InternalComment,
                SalesPerson = request.SalesPerson,
                Currency = request.Currency,
                PurchaseOrderNumber = request.PurchaseOrderNumber,
                Requisitioner = request.Requisitioner,
                ShippedVia = request.ShippedVia,
                Fob = request.Fob,
                Terms = request.Terms,
                BillingAddressRecipient = request.BillingAddressRecipient,
                BillingAddressCompany = request.BillingAddressCompany,
                BillingAddressBuilding = request.BillingAddressBuilding,
                BillingAddressLine1 = request.BillingAddressLine1,
                BillingAddressLine2 = request.BillingAddressLine2,
                BillingAddressCity = request.BillingAddressCity,
                BillingAddressState = request.BillingAddressState,
                BillingAddressPostalCode = request.BillingAddressPostalCode,
                BillingAddressCountry = request.BillingAddressCountry,
                ShippingAddressRecipient = request.ShippingAddressRecipient,
                ShippingAddressRecipientTelephone = request.ShippingAddressRecipientTelephone,
                ShippingAddressCompany = request.ShippingAddressCompany,
                ShippingAddressBuilding = request.ShippingAddressBuilding,
                ShippingAddressLine1 = request.ShippingAddressLine1,
                ShippingAddressLine2 = request.ShippingAddressLine2,
                ShippingAddressCity = request.ShippingAddressCity,
                ShippingAddressState = request.ShippingAddressState,
                ShippingAddressPostalCode = request.ShippingAddressPostalCode,
                ShippingAddressCountry = request.ShippingAddressCountry,
                CommercialRegistration = request.CommercialRegistration,
                TaxIdentification = request.TaxIdentification,
                Subtotal = request.Subtotal,
                Vat = request.Vat,
                Total = request.Total,
                WithholdingTax = request.WithholdingTax,
                Outstanding = request.Outstanding,
                IsPaid = request.IsPaid,
                ReceiptId = request.ReceiptId,
                PaymentDate = request.PaymentDate,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
                InvoiceFiles = new HashSet<InvoiceFile>(),
                OrderItems = new HashSet<OrderItem>()
            };

            await _context.Invoices.AddAsync(invoice);
            await _context.SaveChangesAsync();

            return new InvoiceDto
            {
                Id = invoice.Id,
                Number = invoice.Number,
                CustomerId = invoice.CustomerId,
                Comment = invoice.Comment,
                InternalComment = invoice.InternalComment,
                SalesPerson = invoice.SalesPerson,
                Currency = invoice.Currency,
                PurchaseOrderNumber = invoice.PurchaseOrderNumber,
                Requisitioner = invoice.Requisitioner,
                ShippedVia = invoice.ShippedVia,
                Fob = invoice.Fob,
                Terms = invoice.Terms,
                BillingAddressRecipient = invoice.BillingAddressRecipient,
                BillingAddressCompany = invoice.BillingAddressCompany,
                BillingAddressBuilding = invoice.BillingAddressBuilding,
                BillingAddressLine1 = invoice.BillingAddressLine1,
                BillingAddressLine2 = invoice.BillingAddressLine2,
                BillingAddressCity = invoice.BillingAddressCity,
                BillingAddressState = invoice.BillingAddressState,
                BillingAddressPostalCode = invoice.BillingAddressPostalCode,
                BillingAddressCountry = invoice.BillingAddressCountry,
                ShippingAddressRecipient = invoice.ShippingAddressRecipient,
                ShippingAddressRecipientTelephone = invoice.ShippingAddressRecipientTelephone,
                ShippingAddressCompany = invoice.ShippingAddressCompany,
                ShippingAddressBuilding = invoice.ShippingAddressBuilding,
                ShippingAddressLine1 = invoice.ShippingAddressLine1,
                ShippingAddressLine2 = invoice.ShippingAddressLine2,
                ShippingAddressCity = invoice.ShippingAddressCity,
                ShippingAddressState = invoice.ShippingAddressState,
                ShippingAddressPostalCode = invoice.ShippingAddressPostalCode,
                ShippingAddressCountry = invoice.ShippingAddressCountry,
                CommercialRegistration = invoice.CommercialRegistration,
                TaxIdentification = invoice.TaxIdentification,
                Subtotal = invoice.Subtotal,
                Vat = invoice.Vat,
                Total = invoice.Total,
                WithholdingTax = invoice.WithholdingTax,
                Outstanding = invoice.Outstanding,
                IsPaid = invoice.IsPaid,
                ReceiptId = invoice.ReceiptId,
                PaymentDate = invoice.PaymentDate,
                CreatedDate = invoice.CreatedDate,
                ModifiedDate = invoice.ModifiedDate
            };
        }

        public async Task<bool> DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return false;
            }

            _context.Invoices.Remove(invoice);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<PaginatedResponse<InvoiceDto>> GetAllInvoicesAsync(InvoicePaginationRequest request)
        {
            var query = _context.Invoices.OrderBy(i => i.Number).AsQueryable();

            var totalCount = await query.CountAsync();

            var invoices = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var invoiceDtos = invoices.Select(i => new InvoiceDto
            {
                Id = i.Id,
                Number = i.Number,
                CustomerId = i.CustomerId,
                Comment = i.Comment,
                InternalComment = i.InternalComment,
                SalesPerson = i.SalesPerson,
                Currency = i.Currency,
                PurchaseOrderNumber = i.PurchaseOrderNumber,
                Requisitioner = i.Requisitioner,
                ShippedVia = i.ShippedVia,
                Fob = i.Fob,
                Terms = i.Terms,
                BillingAddressRecipient = i.BillingAddressRecipient,
                BillingAddressCompany = i.BillingAddressCompany,
                BillingAddressBuilding = i.BillingAddressBuilding,
                BillingAddressLine1 = i.BillingAddressLine1,
                BillingAddressLine2 = i.BillingAddressLine2,
                BillingAddressCity = i.BillingAddressCity,
                BillingAddressState = i.BillingAddressState,
                BillingAddressPostalCode = i.BillingAddressPostalCode,
                BillingAddressCountry = i.BillingAddressCountry,
                ShippingAddressRecipient = i.ShippingAddressRecipient,
                ShippingAddressRecipientTelephone = i.ShippingAddressRecipientTelephone,
                ShippingAddressCompany = i.ShippingAddressCompany,
                ShippingAddressBuilding = i.ShippingAddressBuilding,
                ShippingAddressLine1 = i.ShippingAddressLine1,
                ShippingAddressLine2 = i.ShippingAddressLine2,
                ShippingAddressCity = i.ShippingAddressCity,
                ShippingAddressState = i.ShippingAddressState,
                ShippingAddressPostalCode = i.ShippingAddressPostalCode,
                ShippingAddressCountry = i.ShippingAddressCountry,
                CommercialRegistration = i.CommercialRegistration,
                TaxIdentification = i.TaxIdentification,
                Subtotal = i.Subtotal,
                Vat = i.Vat,
                Total = i.Total,
                WithholdingTax = i.WithholdingTax,
                Outstanding = i.Outstanding,
                IsPaid = i.IsPaid,
                ReceiptId = i.ReceiptId,
                PaymentDate = i.PaymentDate,
                CreatedDate = i.CreatedDate,
                ModifiedDate = i.ModifiedDate
            }).ToList();

            return new PaginatedResponse<InvoiceDto>(request.PageNumber, request.PageSize, totalCount, invoiceDtos);
        }

        public async Task<InvoiceDto?> GetInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return null;
            }

            return new InvoiceDto
            {
                Id = invoice.Id,
                Number = invoice.Number,
                CustomerId = invoice.CustomerId,
                Comment = invoice.Comment,
                InternalComment = invoice.InternalComment,
                SalesPerson = invoice.SalesPerson,
                Currency = invoice.Currency,
                PurchaseOrderNumber = invoice.PurchaseOrderNumber,
                Requisitioner = invoice.Requisitioner,
                ShippedVia = invoice.ShippedVia,
                Fob = invoice.Fob,
                Terms = invoice.Terms,
                BillingAddressRecipient = invoice.BillingAddressRecipient,
                BillingAddressCompany = invoice.BillingAddressCompany,
                BillingAddressBuilding = invoice.BillingAddressBuilding,
                BillingAddressLine1 = invoice.BillingAddressLine1,
                BillingAddressLine2 = invoice.BillingAddressLine2,
                BillingAddressCity = invoice.BillingAddressCity,
                BillingAddressState = invoice.BillingAddressState,
                BillingAddressPostalCode = invoice.BillingAddressPostalCode,
                BillingAddressCountry = invoice.BillingAddressCountry,
                ShippingAddressRecipient = invoice.ShippingAddressRecipient,
                ShippingAddressRecipientTelephone = invoice.ShippingAddressRecipientTelephone,
                ShippingAddressCompany = invoice.ShippingAddressCompany,
                ShippingAddressBuilding = invoice.ShippingAddressBuilding,
                ShippingAddressLine1 = invoice.ShippingAddressLine1,
                ShippingAddressLine2 = invoice.ShippingAddressLine2,
                ShippingAddressCity = invoice.ShippingAddressCity,
                ShippingAddressState = invoice.ShippingAddressState,
                ShippingAddressPostalCode = invoice.ShippingAddressPostalCode,
                ShippingAddressCountry = invoice.ShippingAddressCountry,
                CommercialRegistration = invoice.CommercialRegistration,
                TaxIdentification = invoice.TaxIdentification,
                Subtotal = invoice.Subtotal,
                Vat = invoice.Vat,
                Total = invoice.Total,
                WithholdingTax = invoice.WithholdingTax,
                Outstanding = invoice.Outstanding,
                IsPaid = invoice.IsPaid,
                ReceiptId = invoice.ReceiptId,
                PaymentDate = invoice.PaymentDate,
                CreatedDate = invoice.CreatedDate,
                ModifiedDate = invoice.ModifiedDate
            };
        }

        

        public async Task<InvoiceDto?> GetInvoiceByNumberAsync(string number)
        {
            var invoice = await _context.Invoices.SingleOrDefaultAsync(i => i.Number == number);
            if (invoice == null)
            {
                return null;
            }

            return new InvoiceDto
            {
                Id = invoice.Id,
                Number = invoice.Number,
                CustomerId = invoice.CustomerId,
                Comment = invoice.Comment,
                InternalComment = invoice.InternalComment,
                SalesPerson = invoice.SalesPerson,
                Currency = invoice.Currency,
                PurchaseOrderNumber = invoice.PurchaseOrderNumber,
                Requisitioner = invoice.Requisitioner,
                ShippedVia = invoice.ShippedVia,
                Fob = invoice.Fob,
                Terms = invoice.Terms,
                BillingAddressRecipient = invoice.BillingAddressRecipient,
                BillingAddressCompany = invoice.BillingAddressCompany,
                BillingAddressBuilding = invoice.BillingAddressBuilding,
                BillingAddressLine1 = invoice.BillingAddressLine1,
                BillingAddressLine2 = invoice.BillingAddressLine2,
                BillingAddressCity = invoice.BillingAddressCity,
                BillingAddressState = invoice.BillingAddressState,
                BillingAddressPostalCode = invoice.BillingAddressPostalCode,
                BillingAddressCountry = invoice.BillingAddressCountry,
                ShippingAddressRecipient = invoice.ShippingAddressRecipient,
                ShippingAddressRecipientTelephone = invoice.ShippingAddressRecipientTelephone,
                ShippingAddressCompany = invoice.ShippingAddressCompany,
                ShippingAddressBuilding = invoice.ShippingAddressBuilding,
                ShippingAddressLine1 = invoice.ShippingAddressLine1,
                ShippingAddressLine2 = invoice.ShippingAddressLine2,
                ShippingAddressCity = invoice.ShippingAddressCity,
                ShippingAddressState = invoice.ShippingAddressState,
                ShippingAddressPostalCode = invoice.ShippingAddressPostalCode,
                ShippingAddressCountry = invoice.ShippingAddressCountry,
                CommercialRegistration = invoice.CommercialRegistration,
                TaxIdentification = invoice.TaxIdentification,
                Subtotal = invoice.Subtotal,
                Vat = invoice.Vat,
                Total = invoice.Total,
                WithholdingTax = invoice.WithholdingTax,
                Outstanding = invoice.Outstanding,
                IsPaid = invoice.IsPaid,
                ReceiptId = invoice.ReceiptId,
                PaymentDate = invoice.PaymentDate,
                CreatedDate = invoice.CreatedDate,
                ModifiedDate = invoice.ModifiedDate
            };
        }

        public async Task<bool> UpdateInvoiceAsync(int id, UpdateInvoiceRequest request)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice == null)
            {
                return false;
            }

            invoice.Number = request.Number;
            invoice.CustomerId = request.CustomerId;
            invoice.Comment = request.Comment;
            invoice.InternalComment = request.InternalComment;
            invoice.SalesPerson = request.SalesPerson;
            invoice.Currency = request.Currency;
            invoice.PurchaseOrderNumber = request.PurchaseOrderNumber;
            invoice.Requisitioner = request.Requisitioner;
            invoice.ShippedVia = request.ShippedVia;
            invoice.Fob = request.Fob;
            invoice.Terms = request.Terms;
            invoice.BillingAddressRecipient = request.BillingAddressRecipient;
            invoice.BillingAddressCompany = request.BillingAddressCompany;
            invoice.BillingAddressBuilding = request.BillingAddressBuilding;
            invoice.BillingAddressLine1 = request.BillingAddressLine1;
            invoice.BillingAddressLine2 = request.BillingAddressLine2;
            invoice.BillingAddressCity = request.BillingAddressCity;
            invoice.BillingAddressState = request.BillingAddressState;
            invoice.BillingAddressPostalCode = request.BillingAddressPostalCode;
            invoice.BillingAddressCountry = request.BillingAddressCountry;
            invoice.ShippingAddressRecipient = request.ShippingAddressRecipient;
            invoice.ShippingAddressRecipientTelephone = request.ShippingAddressRecipientTelephone;
            invoice.ShippingAddressCompany = request.ShippingAddressCompany;
            invoice.ShippingAddressBuilding = request.ShippingAddressBuilding;
            invoice.ShippingAddressLine1 = request.ShippingAddressLine1;
            invoice.ShippingAddressLine2 = request.ShippingAddressLine2;
            invoice.ShippingAddressCity = request.ShippingAddressCity;
            invoice.ShippingAddressState = request.ShippingAddressState;
            invoice.ShippingAddressPostalCode = request.ShippingAddressPostalCode;
            invoice.ShippingAddressCountry = request.ShippingAddressCountry;
            invoice.CommercialRegistration = request.CommercialRegistration;
            invoice.TaxIdentification = request.TaxIdentification;
            invoice.Subtotal = request.Subtotal;
            invoice.Vat = request.Vat;
            invoice.Total = request.Total;
            invoice.WithholdingTax = request.WithholdingTax;
            invoice.Outstanding = request.Outstanding;
            invoice.IsPaid = request.IsPaid;
            invoice.ReceiptId = request.ReceiptId;
            invoice.PaymentDate = request.PaymentDate;
            invoice.ModifiedDate = DateTime.UtcNow;

            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}