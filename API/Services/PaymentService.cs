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
        /// Processes a payment for an invoice.
        /// </summary>
        /// <param name="paymentDTO">The payment details.</param>
        /// <returns>The updated invoice if the payment was successful, null otherwise.</returns>
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

        /// <summary>
        /// Checks the status of an invoice.
        /// </summary>
        /// <param name="invoiceNumber">The invoice number.</param>
        /// <param name="userId">The user's ID.</param>
        /// <returns>The invoice if found and not paid, otherwise throws an exception.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the invoice is not found or already paid.</exception>
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
