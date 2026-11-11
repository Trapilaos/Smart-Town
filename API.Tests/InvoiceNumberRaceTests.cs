using API.Data;
using API.DTOs;
using API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace API.Tests;

/// <summary>Concurrent invoice creates over one SQLite file, each with its own DbContext, as separate requests would have.</summary>
public class InvoiceNumberRaceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"smarttown-invoices-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<DataContext> _options;

    public InvoiceNumberRaceTests()
    {
        _options = new DbContextOptionsBuilder<DataContext>().UseSqlite($"Data Source={_path}").Options;

        using var context = new DataContext(_options);
        context.Database.EnsureCreated();
    }

    [Fact]
    public async Task Ten_concurrent_creates_get_ten_distinct_numbers_without_an_exception()
    {
        var released = new Barrier(10);
        var creates = Enumerable.Range(0, 10).Select(i => Task.Factory.StartNew(() =>
        {
            released.SignalAndWait();
            return CreateFromOwnContextAsync(i).GetAwaiter().GetResult();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

        var invoices = await Task.WhenAll(creates);

        var numbers = invoices.Select(i => i.InvoiceNumber).ToList();
        Assert.Equal(10, numbers.Distinct().Count());
        Assert.All(numbers, n => Assert.Matches(@"^INV-\d{4}-\d{6}$", n));
    }

    private async Task<AdminInvoiceDTO> CreateFromOwnContextAsync(int index)
    {
        await using var context = new DataContext(_options);
        var service = new InvoiceService(context);

        return await service.CreateAsync(new InvoiceInputDTO
        {
            PayerName = $"Payer {index}",
            Description = "Race test",
            Amount = 10m,
            DueDate = DateTime.UtcNow.AddDays(30),
        });
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        try
        {
            File.Delete(_path);
        }
        catch (IOException)
        {
            // Temporary file; the OS removes it eventually if it is still locked.
        }
    }
}
