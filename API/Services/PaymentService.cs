using API.DTOs;
using Microsoft.EntityFrameworkCore;
using API.Entities;
using API.Data;

namespace API.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly DataContext _context;

        public PaymentService(DataContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Marks the caller's own unpaid invoice as paid. Returns null if there is no such unpaid invoice.
        /// </summary>
        public async Task<Invoice> ProcessPayment(PaymentDTO paymentDTO)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i =>
                i.InvoiceNumber == paymentDTO.InvoiceNumber && i.UserId == paymentDTO.UserId && !i.IsPaid);

            if (invoice != null)
            {
                invoice.IsPaid = true;
                await _context.SaveChangesAsync();
            }

            return invoice;
        }

        /// <summary>
        /// Returns the caller's own invoice (paid or not), or null if it does not exist.
        /// </summary>
        public async Task<Invoice> CheckInvoice(string invoiceNumber, string userId)
        {
            return await _context.Invoices.AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber && i.UserId == userId);
        }
    }
}
