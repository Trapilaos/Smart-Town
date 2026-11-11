using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.Tests;

[Collection(ApiCollection.Name)]
public class CommentsTests
{
    private readonly HttpClient _client;

    public CommentsTests(ApiFactory factory) => _client = factory.CreateApiClient();

    [Fact]
    public async Task Member_cannot_pre_mark_a_comment_as_seen()
    {
        var member = await TestHelpers.RegisterMemberAsync(_client);
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var content = $"hello {Guid.NewGuid():N}";

        var post = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/comments", member,
            new { content, seen = true }));
        Assert.Equal(HttpStatusCode.OK, post.StatusCode);

        // GET /api/comments lists unseen comments only, so a forced Seen=false makes it appear.
        var list = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/comments", admin));
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        var comments = await list.Content.ReadFromJsonAsync<JsonElement>();
        var mine = comments.EnumerateArray().SingleOrDefault(c => c.GetProperty("content").GetString() == content);
        Assert.Equal(JsonValueKind.Object, mine.ValueKind);
        Assert.False(mine.GetProperty("seen").GetBoolean());
    }
}
