using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Errors;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace API.Services
{
    public class InvoiceService : IInvoiceService
    {
        /// <summary>No 0, 1, I or O, so codes read clearly on a printed bill.</summary>
        public const string CodeAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        public const int PaymentCodeLength = 8;

        /// <summary>Compared when the invoice number is unknown, so unknown and wrong-code lookups take the same time.</summary>
        private const string DecoyCode = "ZZZZZZZZ";

        /// <summary>How many times a create retries when another request took the same number first.</summary>
        private const int MaxNumberAttempts = 5;

        /// <summary>
        /// Numbering and saving run one at a time per process, so concurrent creates take distinct numbers
        /// without relying on retries. The unique index on InvoiceNumber is still the final check.
        /// </summary>
        private static readonly SemaphoreSlim NumberingGate = new(1, 1);

        private readonly DataContext _context;

        public InvoiceService(DataContext context)
        {
            _context = context;
        }

        public static string NewPaymentCode()
        {
            var chars = new char[PaymentCodeLength];
            for (var i = 0; i < chars.Length; i++)
            {
                chars[i] = CodeAlphabet[RandomNumberGenerator.GetInt32(CodeAlphabet.Length)];
            }
            return new string(chars);
        }

        public static string NewReceiptReference() => "RCP-" + RandomNumberGenerator.GetHexString(12);

        /// <summary>Constant-time comparison. Codes are case-insensitive.</summary>
        public static bool CodeMatches(string supplied, string stored)
        {
            var a = Encoding.UTF8.GetBytes((supplied ?? string.Empty).Trim().ToUpperInvariant());
            var b = Encoding.UTF8.GetBytes(stored);
            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        public async Task<PublicInvoiceDTO> LookupAsync(string invoiceNumber, string paymentCode)
        {
            var invoice = await FindWithCodeAsync(invoiceNumber, paymentCode);
            return invoice == null ? null : PublicInvoiceDTO.From(invoice);
        }

        public async Task<PayResult> PayWithPaymentCodeAsync(string invoiceNumber, string paymentCode)
        {
            var invoice = await FindWithCodeAsync(invoiceNumber, paymentCode);
            return invoice == null
                ? new PayResult(PayOutcome.NotFound, null)
                : await MarkPaidAsync(invoice.Id);
        }

        public async Task<List<MemberInvoiceDTO>> GetForUserAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            var invoices = await _context.Invoices.AsNoTracking()
                .Where(i => i.UserId == userId)
                .OrderByDescending(i => i.Id)
                .ToListAsync();

            return invoices.Select(i => MemberInvoiceDTO.From(i, today)).ToList();
        }

        public async Task<PayResult> PayAsOwnerAsync(string userId, string invoiceNumber)
        {
            var invoice = await _context.Invoices.AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber && i.UserId == userId);

            return invoice == null
                ? new PayResult(PayOutcome.NotFound, null)
                : await MarkPaidAsync(invoice.Id);
        }

        public async Task<List<AdminInvoiceDTO>> GetForAdminAsync(string status)
        {
            var today = DateTime.UtcNow.Date;

            IQueryable<Invoice> query = _context.Invoices.AsNoTracking();
            query = status switch
            {
                "paid" => query.Where(i => i.IsPaid),
                "unpaid" => query.Where(i => !i.IsPaid),
                "overdue" => query.Where(i => !i.IsPaid && i.DueDate < today),
                _ => query,
            };

            var invoices = await query.OrderByDescending(i => i.Id).ToListAsync();
            var usernames = await UsernameLookup.LoadAsync(_context);

            return invoices.Select(i => AdminInvoiceDTO.From(i, today,
                i.UserId == null ? null : usernames.GetValueOrDefault(i.UserId), includeCode: false)).ToList();
        }

        public async Task<AdminInvoiceDTO> CreateAsync(InvoiceInputDTO input)
        {
            string username = null;
            if (input.UserId.HasValue)
            {
                var user = await _context.Users.AsNoTracking()
                    .Where(u => u.Id == input.UserId.Value)
                    .Select(u => new { u.UserName })
                    .FirstOrDefaultAsync();

                if (user == null) throw new DomainValidationException("No user has that id");
                username = user.UserName;
            }

            var now = DateTime.UtcNow;
            var paymentCode = NewPaymentCode();

            await NumberingGate.WaitAsync();
            try
            {
                for (var attempt = 1; ; attempt++)
                {
                    var invoice = new Invoice
                    {
                        InvoiceNumber = await NextInvoiceNumberAsync(now.Year),
                        Amount = decimal.Round(input.Amount, 2),
                        UserId = input.UserId?.ToString(CultureInfo.InvariantCulture),
                        PayerName = input.PayerName.Trim(),
                        Description = input.Description.Trim(),
                        IssuedAt = now,
                        DueDate = input.DueDate.ToUtc().Date,
                        IsPaid = false,
                        PaymentCode = paymentCode,
                    };

                    _context.Invoices.Add(invoice);
                    try
                    {
                        await _context.SaveChangesAsync();
                        return AdminInvoiceDTO.From(invoice, now.Date, username, includeCode: true);
                    }
                    catch (DbUpdateException ex) when (attempt < MaxNumberAttempts && IsInvoiceNumberClash(ex))
                    {
                        // Another writer took this number. Drop the failed entity and number it again.
                        _context.Entry(invoice).State = EntityState.Detached;
                    }
                }
            }
            finally
            {
                NumberingGate.Release();
            }
        }

        private static bool IsInvoiceNumberClash(DbUpdateException ex) =>
            ex.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 } sqlite
            && sqlite.Message.Contains("InvoiceNumber", StringComparison.Ordinal);

        public async Task<InvoiceSummaryDTO> GetSummaryAsync()
        {
            var today = DateTime.UtcNow.Date;
            var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

            var invoices = _context.Invoices.AsNoTracking();
            var unpaid = invoices.Where(i => !i.IsPaid);

            // SQLite cannot SUM a decimal, so the amounts are summed as doubles in SQL and rounded back to cents.
            // Each amount has two decimal places, so the rounded total is exact for any realistic sum.
            return new InvoiceSummaryDTO
            {
                UnpaidCount = await unpaid.CountAsync(),
                UnpaidTotal = ToCents(await unpaid.SumAsync(i => (double)i.Amount)),
                OverdueCount = await unpaid.CountAsync(i => i.DueDate < today),
                PaidThisMonthTotal = ToCents(await invoices
                    .Where(i => i.IsPaid && i.PaidAt >= monthStart)
                    .SumAsync(i => (double)i.Amount)),
            };
        }

        private static decimal ToCents(double amount) => decimal.Round((decimal)amount, 2, MidpointRounding.AwayFromZero);

        /// <summary>The invoice when the number exists and the code matches, otherwise null.</summary>
        private async Task<Invoice> FindWithCodeAsync(string invoiceNumber, string paymentCode)
        {
            var invoice = await _context.Invoices.AsNoTracking()
                .FirstOrDefaultAsync(i => i.InvoiceNumber == invoiceNumber);

            // Always compare, even for an unknown number, so the timing does not reveal which numbers exist.
            var codeMatches = CodeMatches(paymentCode, invoice?.PaymentCode ?? DecoyCode);
            return invoice != null && invoice.PaymentCode != null && codeMatches ? invoice : null;
        }

        /// <summary>
        /// Marks the invoice paid in one conditional UPDATE, so two simultaneous payments cannot both succeed.
        /// </summary>
        private async Task<PayResult> MarkPaidAsync(int invoiceId)
        {
            var paidAt = DateTime.UtcNow;
            var reference = NewReceiptReference();

            var updated = await _context.Invoices
                .Where(i => i.Id == invoiceId && !i.IsPaid)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(i => i.IsPaid, true)
                    .SetProperty(i => i.PaidAt, (DateTime?)paidAt)
                    .SetProperty(i => i.ReceiptReference, reference));

            return updated == 0
                ? new PayResult(PayOutcome.AlreadyPaid, null)
                : new PayResult(PayOutcome.Paid, new PaymentReceiptDTO { ReceiptReference = reference, PaidAt = paidAt });
        }

        /// <summary>INV-{year}-{six digits}, one above the highest number already issued that year.</summary>
        private async Task<string> NextInvoiceNumberAsync(int year)
        {
            var prefix = $"INV-{year}-";

            // Numbers are zero-padded to a fixed width, so the largest string is also the largest number.
            var last = await _context.Invoices.AsNoTracking()
                .Where(i => i.InvoiceNumber.StartsWith(prefix))
                .OrderByDescending(i => i.InvoiceNumber)
                .Select(i => i.InvoiceNumber)
                .FirstOrDefaultAsync();

            var highest = last != null
                && int.TryParse(last[prefix.Length..], NumberStyles.None, CultureInfo.InvariantCulture, out var value)
                ? value
                : 0;

            return prefix + (highest + 1).ToString("D6", CultureInfo.InvariantCulture);
        }
    }
}
