using API.DTOs;
using Microsoft.EntityFrameworkCore;
using API.Entities;

namespace API.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly DataContext _context;

        public PaymentService(DataContext context)
        {
            _context = context;
        }

        public async Task<Invoice> ProcessPayment(PaymentDTO paymentDTO)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceNumber == paymentDTO.InvoiceNumber && i.UserId == paymentDTO.UserId && !i.IsPaid);

            if (invoice != null)
            {
                invoice.IsPaid = true;
                _context.Invoices.Update(invoice);
                await _context.SaveChangesAsync();
            }

            return invoice;
        }

        public async Task<Invoice> CheckInvoice(string invoiceNumber, string userId)
        {
            var invoice = await _context.Invoices.FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber && i.UserId == userId);
            if (invoice == null)
            {
                throw new InvalidOperationException("Invoice not found");
            }
            if (invoice.IsPaid)
            {
                throw new InvalidOperationException("Invoice already paid");
            }
            return invoice;
        }
    }
}
