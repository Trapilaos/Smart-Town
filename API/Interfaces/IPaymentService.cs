using API.DTOs;
using API.Entities;


namespace API.Services
{
    public interface IPaymentService
    {
        Task<Invoice> ProcessPayment(PaymentDTO paymentDTO);
        Task<Invoice> CheckInvoice(string invoiceNumber, string userId);
    }
}
