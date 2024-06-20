
namespace API.Entities
{
    public class Invoice
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public string UserId { get; set; }
        public bool IsPaid { get; set; }
    }
}