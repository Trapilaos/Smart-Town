using API.Data;
using API.Entities;
using API.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace API.Tests;

/// <summary>Two interest requests for the same event and member at once, each with its own DbContext.</summary>
public class EventInterestRaceTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"smarttown-interest-{Guid.NewGuid():N}.db");
    private readonly DbContextOptions<DataContext> _options;

    public EventInterestRaceTests()
    {
        _options = new DbContextOptionsBuilder<DataContext>().UseSqlite($"Data Source={_path}").Options;

        using var context = new DataContext(_options);
        context.Database.EnsureCreated();
        context.Events.Add(new Event { Id = 1, Title = "Race", Date = DateTime.UtcNow.AddDays(3), IsPublished = true, CreatedAt = DateTime.UtcNow });
        context.SaveChanges();
    }

    [Fact]
    public async Task Two_concurrent_interest_requests_both_succeed_and_record_one_interest()
    {
        var released = new Barrier(2);
        var requests = Enumerable.Range(0, 2).Select(_ => Task.Factory.StartNew(() =>
        {
            released.SignalAndWait();
            return SetInterestFromOwnContextAsync().GetAwaiter().GetResult();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default)).ToArray();

        var results = await Task.WhenAll(requests);

        Assert.All(results, dto => Assert.NotNull(dto));
        Assert.All(results, dto => Assert.True(dto.IsInterested));

        await using var check = new DataContext(_options);
        Assert.Equal(1, await check.EventInterests.CountAsync());
        Assert.Equal(1, (await check.Events.SingleAsync()).Id);
    }

    private async Task<API.DTOs.EventDTO> SetInterestFromOwnContextAsync()
    {
        await using var context = new DataContext(_options);
        return await new EventService(context).SetInterestAsync(1, "member-1", interested: true);
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
