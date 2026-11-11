using API.Entities;
using API.Helpers;
using API.Services;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class InvoiceSeed
    {
        private static readonly string[] Descriptions =
        {
            "Water and sewerage, quarter 1",
            "Waste collection fee, annual",
            "Parking permit renewal",
        };

        /// <summary>
        /// Seeds 24 demo invoices (three for each demo resident) when demo data is enabled and the table is empty, then backfills
        /// any invoice that predates the description, due date, payment code and receipt columns.
        /// </summary>
        public static async Task SeedInvoices(DataContext context, bool demoData)
        {
            if (demoData && !await context.Invoices.AnyAsync())
            {
                await context.Invoices.AddRangeAsync(Generate(DateTime.UtcNow));
                await context.SaveChangesAsync();
            }

            await BackfillAsync(context);
        }

        /// <summary>
        /// Per resident: one paid invoice, one unpaid and overdue, and one unpaid and not yet due.
        /// Payment codes are random, so they differ between databases.
        /// </summary>
        public static List<Invoice> Generate(DateTime nowUtc)
        {
            var today = nowUtc.Date;
            var invoices = new List<Invoice>();
            var sequence = 0;

            for (var userId = 1; userId <= 8; userId++)
            {
                for (var i = 0; i < 3; i++)
                {
                    sequence++;
                    var issued = today.AddDays(-45 + i);
                    var isPaid = i == 0;

                    invoices.Add(new Invoice
                    {
                        InvoiceNumber = $"INV-{nowUtc.Year}-{sequence:D6}",
                        Amount = decimal.Round(40m + sequence * 7.35m % 85m, 2),
                        UserId = userId.ToString(),
                        Description = Descriptions[i],
                        IssuedAt = issued,
                        DueDate = i switch { 0 => today.AddDays(-20), 1 => today.AddDays(-5), _ => today.AddDays(30) },
                        IsPaid = isPaid,
                        PaidAt = isPaid ? issued.AddDays(7) : null,
                        PaymentCode = InvoiceService.NewPaymentCode(),
                        ReceiptReference = isPaid ? InvoiceService.NewReceiptReference() : null,
                    });
                }
            }

            return invoices;
        }

        private static async Task BackfillAsync(DataContext context)
        {
            var incomplete = await context.Invoices
                .Where(i => i.PaymentCode == null || i.Description == null || i.IssuedAt == null
                    || i.DueDate == null || i.PayerName == null
                    || (i.IsPaid && (i.PaidAt == null || i.ReceiptReference == null)))
                .ToListAsync();

            if (incomplete.Count == 0) return;

            var now = DateTime.UtcNow;
            var usernames = await UsernameLookup.LoadAsync(context);

            foreach (var invoice in incomplete)
            {
                // Invoices from the first seed carried unrounded random amounts.
                invoice.Amount = decimal.Round(invoice.Amount, 2);
                invoice.Description ??= "Municipal bill";
                invoice.IssuedAt ??= now;
                invoice.DueDate ??= invoice.IssuedAt.Value.AddDays(30);
                invoice.PayerName ??= PayerFor(invoice.UserId, usernames);
                invoice.PaymentCode ??= InvoiceService.NewPaymentCode();

                if (invoice.IsPaid)
                {
                    invoice.PaidAt ??= invoice.IssuedAt;
                    invoice.ReceiptReference ??= InvoiceService.NewReceiptReference();
                }
            }

            await context.SaveChangesAsync();
        }

        private static string PayerFor(string userId, Dictionary<string, string> usernames) =>
            userId != null && usernames.TryGetValue(userId, out var username) ? username : "Walk-in payer";
    }
}
