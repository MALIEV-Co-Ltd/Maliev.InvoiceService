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
    public class OrderItemService : IOrderItemService
    {
        private readonly InvoiceContext _context;

        public OrderItemService(InvoiceContext context)
        {
            _context = context;
        }

        public async Task<OrderItemDto> CreateOrderItemAsync(CreateOrderItemRequest request)
        {
            var orderItem = new OrderItem
            {
                InvoiceId = request.InvoiceId,
                Description = request.Description,
                Quantity = request.Quantity,
                UnitPrice = request.UnitPrice,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow
            };

            await _context.OrderItems.AddAsync(orderItem);
            await _context.SaveChangesAsync();

            return new OrderItemDto
            {
                Id = orderItem.Id,
                InvoiceId = orderItem.InvoiceId,
                Description = orderItem.Description,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                Subtotal = orderItem.Subtotal,
                CreatedDate = orderItem.CreatedDate,
                ModifiedDate = orderItem.ModifiedDate
            };
        }

        public async Task<bool> DeleteOrderItemAsync(int id)
        {
            var orderItem = await _context.OrderItems.FindAsync(id);
            if (orderItem == null)
            {
                return false;
            }

            _context.OrderItems.Remove(orderItem);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<OrderItemDto>> GetAllOrderItemsAsync()
        {
            var orderItems = await _context.OrderItems.OrderBy(o => o.Description).ToListAsync();
            return orderItems.Select(o => new OrderItemDto
            {
                Id = o.Id,
                InvoiceId = o.InvoiceId,
                Description = o.Description,
                Quantity = o.Quantity,
                UnitPrice = o.UnitPrice,
                Subtotal = o.Subtotal,
                CreatedDate = o.CreatedDate,
                ModifiedDate = o.ModifiedDate
            }).ToList();
        }

        public async Task<List<OrderItemDto>> GetOrderItemsAsync(int invoiceId)
        {
            var orderItems = await _context.OrderItems
                                            .Where(o => o.InvoiceId == invoiceId)
                                            .OrderBy(o => o.Description)
                                            .ToListAsync();
            return orderItems.Select(o => new OrderItemDto
            {
                Id = o.Id,
                InvoiceId = o.InvoiceId,
                Description = o.Description,
                Quantity = o.Quantity,
                UnitPrice = o.UnitPrice,
                Subtotal = o.Subtotal,
                CreatedDate = o.CreatedDate,
                ModifiedDate = o.ModifiedDate
            }).ToList();
        }

        public async Task<OrderItemDto?> GetOrderItemAsync(int id)
        {
            var orderItem = await _context.OrderItems.FindAsync(id);
            if (orderItem == null)
            {
                return null;
            }

            return new OrderItemDto
            {
                Id = orderItem.Id,
                InvoiceId = orderItem.InvoiceId,
                Description = orderItem.Description,
                Quantity = orderItem.Quantity,
                UnitPrice = orderItem.UnitPrice,
                Subtotal = orderItem.Subtotal,
                CreatedDate = orderItem.CreatedDate,
                ModifiedDate = orderItem.ModifiedDate
            };
        }

        public async Task<bool> UpdateOrderItemAsync(int id, UpdateOrderItemRequest request)
        {
            var orderItem = await _context.OrderItems.FindAsync(id);
            if (orderItem == null)
            {
                return false;
            }

            orderItem.InvoiceId = request.InvoiceId;
            orderItem.Description = request.Description;
            orderItem.Quantity = request.Quantity;
            orderItem.UnitPrice = request.UnitPrice;
            orderItem.ModifiedDate = DateTime.UtcNow;

            _context.OrderItems.Update(orderItem);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
