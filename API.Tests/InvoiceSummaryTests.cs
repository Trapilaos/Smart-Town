using API.Data;
using API.Entities;
using API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace API.Tests;

/// <summary>The dashboard totals are computed in SQL. They must still match exactly, cent for cent.</summary>
public class InvoiceSummaryTests : IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly DataContext _context;

    public InvoiceSummaryTests()
    {
        _connection.Open();
        _context = new DataContext(new DbContextOptionsBuilder<DataContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    [Fact]
    public async Task Empty_database_gives_zero_totals()
    {
        var summary = await new InvoiceService(_context).GetSummaryAsync();

        Assert.Equal(0, summary.UnpaidCount);
        Assert.Equal(0m, summary.UnpaidTotal);
        Assert.Equal(0, summary.OverdueCount);
        Assert.Equal(0m, summary.PaidThisMonthTotal);
    }

    [Fact]
    public async Task Totals_match_exactly_for_typical_amounts()
    {
        var today = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        _context.Invoices.AddRange(
            // Unpaid. 10.10 + 20.20 + 0.30 = 30.60 exactly, which plain double arithmetic would not guarantee.
            NewInvoice(amount: 10.10m, isPaid: false, dueDate: today.AddDays(10)),
            NewInvoice(amount: 20.20m, isPaid: false, dueDate: today.AddDays(-3)),          // overdue
            NewInvoice(amount: 0.30m, isPaid: false, dueDate: today.AddDays(-1)),           // overdue
            // Paid this month: 99.99 + 0.01 = 100.00.
            NewInvoice(amount: 99.99m, isPaid: true, paidAt: monthStart.AddHours(1)),
            NewInvoice(amount: 0.01m, isPaid: true, paidAt: DateTime.UtcNow),
            // Paid before this month: not counted.
            NewInvoice(amount: 500m, isPaid: true, paidAt: monthStart.AddDays(-1)));
        await _context.SaveChangesAsync();

        var summary = await new InvoiceService(_context).GetSummaryAsync();

        Assert.Equal(3, summary.UnpaidCount);
        Assert.Equal(30.60m, summary.UnpaidTotal);
        Assert.Equal(2, summary.OverdueCount);
        Assert.Equal(100.00m, summary.PaidThisMonthTotal);
    }

    private static Invoice NewInvoice(decimal amount, bool isPaid, DateTime? dueDate = null, DateTime? paidAt = null) => new()
    {
        InvoiceNumber = $"INV-TEST-{Guid.NewGuid():N}"[..20],
        Amount = amount,
        PayerName = "Test",
        Description = "Test",
        IssuedAt = DateTime.UtcNow,
        DueDate = dueDate ?? DateTime.UtcNow.Date.AddDays(30),
        IsPaid = isPaid,
        PaidAt = paidAt,
        PaymentCode = "ABCDEFGH",
    };
}
