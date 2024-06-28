using API.Entities;

namespace API.Data
{
    public class InvoiceSeed
    {
        public static async Task SeedInvoices(DataContext context)
        {
            if (context.Invoices.Any()) return;

            var invoices = GenerateInvoices();

            await context.Invoices.AddRangeAsync(invoices);
            await context.SaveChangesAsync();
        }

        public static List<Invoice> GenerateInvoices()
        {
            var invoices = new List<Invoice>();

            for (int userId = 1; userId <= 8; userId++)
            {
                for (int i = 0; i < 3; i++)
                {
                    invoices.Add(new Invoice
                    {
                        Id = i + (userId - 1) * 3 + 1,
                        InvoiceNumber = $"INV{userId}-{(i + 1):D2}",
                        Amount = (decimal)(new Random().NextDouble() * 100 + 50),
                        UserId = userId.ToString(),
                        IsPaid = false
                    });
                }
            }

            return invoices;
        }
    }
}
