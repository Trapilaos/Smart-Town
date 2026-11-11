using API.DTOs;

namespace API.Interfaces
{
    public enum PayOutcome
    {
        NotFound,
        AlreadyPaid,
        Paid,
    }

    public record PayResult(PayOutcome Outcome, PaymentReceiptDTO Receipt);

    public interface IInvoiceService
    {
        /// <summary>Returns null for an unknown number or a wrong payment code. Both cases look the same to the caller.</summary>
        Task<PublicInvoiceDTO> LookupAsync(string invoiceNumber, string paymentCode);
        Task<PayResult> PayWithPaymentCodeAsync(string invoiceNumber, string paymentCode);

        Task<List<MemberInvoiceDTO>> GetForUserAsync(string userId);
        /// <summary>Only the owner can pay. Someone else's invoice reports NotFound.</summary>
        Task<PayResult> PayAsOwnerAsync(string userId, string invoiceNumber);

        /// <summary>status is null (all), paid, unpaid or overdue. Callers validate it.</summary>
        Task<List<AdminInvoiceDTO>> GetForAdminAsync(string status);
        /// <summary>Throws <see cref="API.Errors.DomainValidationException"/> when the user does not exist.</summary>
        Task<AdminInvoiceDTO> CreateAsync(InvoiceInputDTO input);
        Task<InvoiceSummaryDTO> GetSummaryAsync();
    }
}
