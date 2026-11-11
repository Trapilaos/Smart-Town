using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace API.Tests;

[Collection(ApiCollection.Name)]
public class EventsTests
{
    private readonly HttpClient _client;

    public EventsTests(ApiFactory factory) => _client = factory.CreateApiClient();

    private static object EventBody(string title, bool published = true, string photoUrl = null, DateTime? date = null) => new
    {
        title,
        description = "Test event",
        photoUrl,
        location = "Central square, Glyfada",
        lat = 37.8660,
        lng = 23.7540,
        date = date ?? DateTime.UtcNow.AddDays(5),
        endDate = (DateTime?)null,
        category = "Culture",
        isPublished = published,
    };

    private async Task<JsonElement> AdminCreateAsync(object body)
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/events", admin, body));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [Fact]
    public async Task Public_list_hides_unpublished_events_and_never_shows_user_ids()
    {
        var title = $"Public {Guid.NewGuid():N}"[..20];
        var published = await AdminCreateAsync(EventBody(title));
        var hidden = await AdminCreateAsync(EventBody($"Hidden {Guid.NewGuid():N}"[..20], published: false));
        var member = await TestHelpers.RegisterMemberAsync(_client);
        await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, $"/api/events/{published.GetProperty("id").GetInt32()}/interest", member));

        var list = await _client.GetFromJsonAsync<JsonElement>("/api/events");

        Assert.Contains(list.EnumerateArray(), e => e.GetProperty("title").GetString() == title);
        Assert.DoesNotContain(list.EnumerateArray(), e => e.GetProperty("id").GetInt32() == hidden.GetProperty("id").GetInt32());
        Assert.DoesNotContain(JsonTestHelpers.AllPropertyNames(list), n => JsonTestHelpers.IdentityFields.Contains(n));
    }

    [Fact]
    public async Task Unpublished_event_is_404_for_the_public_but_listed_for_admins()
    {
        var hidden = await AdminCreateAsync(EventBody($"Draft {Guid.NewGuid():N}"[..20], published: false));
        var id = hidden.GetProperty("id").GetInt32();

        var publicView = await _client.GetAsync($"/api/events/{id}");
        Assert.Equal(HttpStatusCode.NotFound, publicView.StatusCode);

        var admin = await TestHelpers.LoginAdminAsync(_client);
        var adminList = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/admin/events", admin));
        var rows = await adminList.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(rows.EnumerateArray(), e => e.GetProperty("id").GetInt32() == id && !e.GetProperty("isPublished").GetBoolean());
    }

    [Fact]
    public async Task Interest_is_idempotent_and_can_be_withdrawn()
    {
        var created = await AdminCreateAsync(EventBody($"Interest {Guid.NewGuid():N}"[..20]));
        var id = created.GetProperty("id").GetInt32();
        var member = await TestHelpers.RegisterMemberAsync(_client);
        var url = $"/api/events/{id}/interest";

        var first = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, url, member));
        var second = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, url, member));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var afterInterest = await second.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, afterInterest.GetProperty("interestedCount").GetInt32());
        Assert.True(afterInterest.GetProperty("isInterested").GetBoolean());

        var anonymous = await _client.GetFromJsonAsync<JsonElement>($"/api/events/{id}");
        Assert.False(anonymous.GetProperty("isInterested").GetBoolean());
        Assert.Equal(1, anonymous.GetProperty("interestedCount").GetInt32());

        var withdrawn = await _client.SendAsync(TestHelpers.Request(HttpMethod.Delete, url, member));
        var body = await withdrawn.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(body.GetProperty("isInterested").GetBoolean());
        Assert.Equal(0, body.GetProperty("interestedCount").GetInt32());
    }

    [Fact]
    public async Task Interest_in_an_unpublished_event_is_404()
    {
        var hidden = await AdminCreateAsync(EventBody($"Secret {Guid.NewGuid():N}"[..20], published: false));
        var member = await TestHelpers.RegisterMemberAsync(_client);

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post,
            $"/api/events/{hidden.GetProperty("id").GetInt32()}/interest", member));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Photo_must_be_an_https_url()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/events", admin,
            EventBody("Insecure photo", photoUrl: "http://example.com/photo.jpg")));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Dates_are_returned_in_utc_with_a_z_suffix()
    {
        var created = await AdminCreateAsync(EventBody($"Utc {Guid.NewGuid():N}"[..20]));

        Assert.EndsWith("Z", created.GetProperty("date").GetString());
        var fetched = await _client.GetFromJsonAsync<JsonElement>($"/api/events/{created.GetProperty("id").GetInt32()}");
        Assert.EndsWith("Z", fetched.GetProperty("date").GetString());
    }

    [Fact]
    public async Task Past_events_are_hidden_unless_asked_for()
    {
        // The seed includes "Winter lights ceremony", which ended 30 days ago.
        var upcoming = await _client.GetFromJsonAsync<JsonElement>("/api/events");
        var withPast = await _client.GetFromJsonAsync<JsonElement>("/api/events?includePast=true");

        Assert.DoesNotContain(upcoming.EnumerateArray(), e => e.GetProperty("title").GetString() == "Winter lights ceremony");
        Assert.Contains(withPast.EnumerateArray(), e => e.GetProperty("title").GetString() == "Winter lights ceremony");
    }

    [Fact]
    public async Task Unknown_category_filter_is_400()
    {
        var response = await _client.GetAsync("/api/events?category=Plants");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Category_filter_returns_only_that_category()
    {
        var list = await _client.GetFromJsonAsync<JsonElement>("/api/events?category=Sports");

        Assert.All(list.EnumerateArray(), e => Assert.Equal("Sports", e.GetProperty("category").GetString()));
    }
}
