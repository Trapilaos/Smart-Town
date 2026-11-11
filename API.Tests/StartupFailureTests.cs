namespace API.Tests;

/// <summary>A database that cannot be opened must stop startup, not leave a half-ready API running.</summary>
[Collection(ApiCollection.Name)]
public class StartupFailureTests
{
    [Fact]
    public void Migration_failure_stops_startup_instead_of_serving_requests()
    {
        using var factory = new UnreachableDatabaseFactory();

        Assert.ThrowsAny<Exception>(() => factory.CreateApiClient());
    }

    private sealed class UnreachableDatabaseFactory : ApiFactory
    {
        protected override IReadOnlyDictionary<string, string> ExtraSettings => new Dictionary<string, string>
        {
            // The directory does not exist, so SQLite cannot open the file and migration fails.
            ["ConnectionStrings:DefaultConnection"] = @"Data Source=Z:\no-such-directory-for-tests\smarttown.db",
        };
    }
}
