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
    public class InvoiceFileService : IInvoiceFileService
    {
        private readonly InvoiceContext _context;

        public InvoiceFileService(InvoiceContext context)
        {
            _context = context;
        }

        public async Task<InvoiceFileDto> CreateInvoiceFileAsync(CreateInvoiceFileRequest request)
        {
            var invoiceFile = new InvoiceFile
            {
                InvoiceId = request.InvoiceId,
                Bucket = request.Bucket,
                ObjectName = request.ObjectName,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            await _context.InvoiceFiles.AddAsync(invoiceFile);
            await _context.SaveChangesAsync();

            return new InvoiceFileDto
            {
                Id = invoiceFile.Id,
                InvoiceId = invoiceFile.InvoiceId,
                Bucket = invoiceFile.Bucket,
                ObjectName = invoiceFile.ObjectName,
                CreatedDate = invoiceFile.CreatedDate,
                ModifiedDate = invoiceFile.ModifiedDate
            };
        }
        

        public async Task<bool> DeleteInvoiceFileAsync(int invoiceId)
        {
            var invoiceFile = await _context.InvoiceFiles.SingleOrDefaultAsync(f => f.InvoiceId == invoiceId);
            if (invoiceFile == null)
            {
                return false;
            }

            _context.InvoiceFiles.Remove(invoiceFile);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<InvoiceFileDto>> GetAllInvoiceFilesAsync()
        {
            var invoiceFiles = await _context.InvoiceFiles.OrderBy(f => f.ObjectName).ToListAsync();
            return invoiceFiles.Select(f => new InvoiceFileDto
            {
                Id = f.Id,
                InvoiceId = f.InvoiceId,
                Bucket = f.Bucket,
                ObjectName = f.ObjectName,
                CreatedDate = f.CreatedDate,
                ModifiedDate = f.ModifiedDate
            }).ToList();
        }

        public async Task<List<InvoiceFileDto>> GetInvoiceFilesAsync(int invoiceId)
        {
            var invoiceFiles = await _context.InvoiceFiles
                                            .Where(f => f.InvoiceId == invoiceId)
                                            .OrderBy(f => f.ObjectName)
                                            .ToListAsync();
            return invoiceFiles.Select(f => new InvoiceFileDto
            {
                Id = f.Id,
                InvoiceId = f.InvoiceId,
                Bucket = f.Bucket,
                ObjectName = f.ObjectName,
                CreatedDate = f.CreatedDate,
                ModifiedDate = f.ModifiedDate
            }).ToList();
        }

        public async Task<InvoiceFileDto?> GetInvoiceFileAsync(int invoiceId)
        {
            var invoiceFile = await _context.InvoiceFiles.SingleOrDefaultAsync(f => f.InvoiceId == invoiceId);
            if (invoiceFile == null)
            {
                return null;
            }

            return new InvoiceFileDto
            {
                Id = invoiceFile.Id,
                InvoiceId = invoiceFile.InvoiceId,
                Bucket = invoiceFile.Bucket,
                ObjectName = invoiceFile.ObjectName,
                CreatedDate = invoiceFile.CreatedDate,
                ModifiedDate = invoiceFile.ModifiedDate
            };
        }

        public async Task<bool> UpdateInvoiceFileAsync(int invoiceId, UpdateInvoiceFileRequest request)
        {
            var invoiceFile = await _context.InvoiceFiles.SingleOrDefaultAsync(f => f.InvoiceId == invoiceId);
            if (invoiceFile == null)
            {
                return false;
            }

            invoiceFile.InvoiceId = request.InvoiceId;
            invoiceFile.Bucket = request.Bucket;
            invoiceFile.ObjectName = request.ObjectName;
            invoiceFile.ModifiedDate = DateTime.UtcNow;

            _context.InvoiceFiles.Update(invoiceFile);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
