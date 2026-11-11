namespace API.Entities
{
    public class Invoice
    {
        public int Id { get; set; }
        /// <summary>Unique. New invoices are numbered INV-{yyyy}-{6 digits}.</summary>
        public string InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        /// <summary>Owner account, or null for a payer without an account.</summary>
        public string UserId { get; set; }
        /// <summary>Name on the bill, used for payers without an account.</summary>
        public string PayerName { get; set; }
        public string Description { get; set; }
        public DateTime? IssuedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public bool IsPaid { get; set; }
        public DateTime? PaidAt { get; set; }
        /// <summary>8 characters printed on the bill. Needed with the invoice number for public lookup and payment.</summary>
        public string PaymentCode { get; set; }
        public string ReceiptReference { get; set; }
    }
}
