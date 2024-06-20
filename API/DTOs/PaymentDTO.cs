namespace API.DTOs
{
    public class PaymentDTO
    {
        public string UserId { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; }
        public string Description { get; set; }
    }
}