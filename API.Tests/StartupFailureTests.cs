namespace API.Tests;

/// <summary>A database that cannot be opened must stop startup, not leave a half-ready API running.</summary>
[Collection(ApiCollection.Name)]
public class StartupFailureTests
{
    [Fact]
    public void Migration_failure_stops_startup_instead_of_serving_requests()
    {
        // A regular file used as if it were a directory: SQLite cannot create a database "inside" it
        // on any OS. (A Windows-only path such as Z:\... is just a valid relative file name on Linux.)
        var blockingFile = Path.GetTempFileName();
        try
        {
            using var factory = new UnreachableDatabaseFactory(Path.Combine(blockingFile, "smarttown.db"));

            Assert.ThrowsAny<Exception>(() => factory.CreateApiClient());
        }
        finally
        {
            File.Delete(blockingFile);
        }
    }

    private sealed class UnreachableDatabaseFactory : ApiFactory
    {
        private readonly string _databasePath;

        public UnreachableDatabaseFactory(string databasePath)
        {
            _databasePath = databasePath;
        }

        protected override IReadOnlyDictionary<string, string> ExtraSettings => new Dictionary<string, string>
        {
            ["ConnectionStrings:DefaultConnection"] = $"Data Source={_databasePath}",
        };
    }
}
