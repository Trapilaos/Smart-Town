using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using API.Entities;

namespace API.DTOs
{
    /// <summary>Public lookup and pay request. The payment code is the 8 characters printed on the bill.</summary>
    public class PublicInvoiceRequest
    {
        [Required]
        [StringLength(32, MinimumLength = 3)]
        public string InvoiceNumber { get; set; }

        [Required]
        [StringLength(8, MinimumLength = 8)]
        public string PaymentCode { get; set; }
    }

    /// <summary>Invoice details for the public lookup. Never includes the owner or any user data.</summary>
    public class PublicInvoiceDTO
    {
        public string InvoiceNumber { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }

        public static PublicInvoiceDTO From(Invoice invoice) => new()
        {
            InvoiceNumber = invoice.InvoiceNumber,
            Description = invoice.Description,
            Amount = invoice.Amount,
            DueDate = invoice.DueDate,
            IsPaid = invoice.IsPaid,
            PaidAt = invoice.PaidAt,
        };
    }

    public class PaymentReceiptDTO
    {
        public string ReceiptReference { get; set; }
        public DateTime PaidAt { get; set; }
    }

    /// <summary>An invoice in the signed-in member's own list. Includes the payment code because it belongs to them.</summary>
    public class MemberInvoiceDTO
    {
        public string InvoiceNumber { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime? IssuedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public bool IsOverdue { get; set; }
        public string PaymentCode { get; set; }
        public string ReceiptReference { get; set; }

        public static MemberInvoiceDTO From(Invoice invoice, DateTime today) => new()
        {
            InvoiceNumber = invoice.InvoiceNumber,
            Description = invoice.Description,
            Amount = invoice.Amount,
            IssuedAt = invoice.IssuedAt,
            DueDate = invoice.DueDate,
            IsPaid = invoice.IsPaid,
            PaidAt = invoice.PaidAt,
            IsOverdue = InvoiceDTOHelpers.IsOverdue(invoice, today),
            PaymentCode = invoice.PaymentCode,
            ReceiptReference = invoice.ReceiptReference,
        };
    }

    /// <summary>An invoice as the admin sees it. The payment code is only included when the invoice is created.</summary>
    public class AdminInvoiceDTO
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public string PayerName { get; set; }
        /// <summary>Owner's username, or null when the invoice has no account.</summary>
        public string Username { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public DateTime? IssuedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        public bool IsOverdue { get; set; }
        public string ReceiptReference { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string PaymentCode { get; set; }

        public static AdminInvoiceDTO From(Invoice invoice, DateTime today, string username, bool includeCode) => new()
        {
            Id = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            PayerName = invoice.PayerName,
            Username = username,
            Description = invoice.Description,
            Amount = invoice.Amount,
            IssuedAt = invoice.IssuedAt,
            DueDate = invoice.DueDate,
            IsPaid = invoice.IsPaid,
            PaidAt = invoice.PaidAt,
            IsOverdue = InvoiceDTOHelpers.IsOverdue(invoice, today),
            ReceiptReference = invoice.ReceiptReference,
            PaymentCode = includeCode ? invoice.PaymentCode : null,
        };
    }

    /// <summary>Admin create payload. UserId is optional: without it the invoice belongs to a payer without an account.</summary>
    public class InvoiceInputDTO : IValidatableObject
    {
        [Range(1, int.MaxValue)]
        public int? UserId { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 1)]
        public string PayerName { get; set; }

        [Required]
        [StringLength(500, MinimumLength = 1)]
        public string Description { get; set; }

        /// <summary>Between 0.01 and 100000 EUR. Checked in code: a decimal Range attribute parses with the server culture.</summary>
        public decimal Amount { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (Amount < 0.01m || Amount > 100000m)
                yield return new ValidationResult("Amount must be between 0.01 and 100000", new[] { nameof(Amount) });
        }
    }

    public class InvoiceSummaryDTO
    {
        public int UnpaidCount { get; set; }
        public decimal UnpaidTotal { get; set; }
        public int OverdueCount { get; set; }
        public decimal PaidThisMonthTotal { get; set; }
    }

    public static class InvoiceDTOHelpers
    {
        /// <summary>Unpaid and past its due date (compared on UTC dates).</summary>
        public static bool IsOverdue(Invoice invoice, DateTime today) =>
            !invoice.IsPaid && invoice.DueDate.HasValue && invoice.DueDate.Value < today;
    }
}
