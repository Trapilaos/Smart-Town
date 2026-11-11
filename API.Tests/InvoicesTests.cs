using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace API.Tests;

[Collection(ApiCollection.Name)]
public class InvoicesTests
{
    private static readonly Regex PaymentCodeShape = new("^[A-HJ-NP-Z2-9]{8}$");
    private static readonly Regex InvoiceNumberShape = new(@"^INV-\d{4}-\d{6}$");

    private readonly HttpClient _client;

    public InvoicesTests(ApiFactory factory) => _client = factory.CreateApiClient();

    private static object InvoiceBody(decimal amount = 20m, int? userId = null, DateTime? dueDate = null) => new
    {
        userId,
        payerName = "Test payer",
        description = "Test bill",
        amount,
        dueDate = dueDate ?? DateTime.UtcNow.AddDays(14),
    };

    private async Task<JsonElement> AdminCreateAsync(object body)
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/invoices", admin, body));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    private Task<HttpResponseMessage> LookupAsync(string number, string code) =>
        _client.PostAsJsonAsync("/api/public/invoices/lookup", new { invoiceNumber = number, paymentCode = code });

    private Task<HttpResponseMessage> PayAsync(string number, string code) =>
        _client.PostAsJsonAsync("/api/public/invoices/pay", new { invoiceNumber = number, paymentCode = code });

    private static string WrongCodeFor(string code) => code == "AAAAAAAA" ? "BBBBBBBB" : "AAAAAAAA";

    [Fact]
    public async Task Admin_create_returns_a_well_formed_number_and_a_payment_code()
    {
        var invoice = await AdminCreateAsync(InvoiceBody());

        Assert.Matches(InvoiceNumberShape, invoice.GetProperty("invoiceNumber").GetString());
        Assert.Matches(PaymentCodeShape, invoice.GetProperty("paymentCode").GetString());
    }

    [Fact]
    public async Task Admin_list_never_shows_payment_codes()
    {
        var invoice = await AdminCreateAsync(InvoiceBody());
        var number = invoice.GetProperty("invoiceNumber").GetString();
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var list = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/admin/invoices?status=unpaid", admin));
        var rows = await list.Content.ReadFromJsonAsync<JsonElement>();

        var row = rows.EnumerateArray().Single(r => r.GetProperty("invoiceNumber").GetString() == number);
        Assert.False(row.TryGetProperty("paymentCode", out _));
    }

    [Fact]
    public async Task Lookup_with_the_right_code_returns_bill_details_and_no_user_data()
    {
        var invoice = await AdminCreateAsync(InvoiceBody(amount: 33.5m));

        var response = await LookupAsync(invoice.GetProperty("invoiceNumber").GetString(), invoice.GetProperty("paymentCode").GetString());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(33.5m, body.GetProperty("amount").GetDecimal());
        Assert.Equal("Test bill", body.GetProperty("description").GetString());
        Assert.False(body.GetProperty("isPaid").GetBoolean());
        Assert.DoesNotContain(JsonTestHelpers.AllPropertyNames(body), n => JsonTestHelpers.IdentityFields.Contains(n));
    }

    [Fact]
    public async Task Wrong_code_and_unknown_number_return_the_same_404()
    {
        var invoice = await AdminCreateAsync(InvoiceBody());
        var number = invoice.GetProperty("invoiceNumber").GetString();
        var code = invoice.GetProperty("paymentCode").GetString();

        var wrongCode = await LookupAsync(number, WrongCodeFor(code));
        var unknownNumber = await LookupAsync("INV-1999-999999", code);

        Assert.Equal(HttpStatusCode.NotFound, wrongCode.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownNumber.StatusCode);
        Assert.Equal(await wrongCode.Content.ReadAsStringAsync(), await unknownNumber.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Paying_twice_returns_409_and_the_receipt_has_a_reference()
    {
        var invoice = await AdminCreateAsync(InvoiceBody());
        var number = invoice.GetProperty("invoiceNumber").GetString();
        var code = invoice.GetProperty("paymentCode").GetString();

        var first = await PayAsync(number, code);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var receipt = await first.Content.ReadFromJsonAsync<JsonElement>();
        Assert.StartsWith("RCP-", receipt.GetProperty("receiptReference").GetString());
        Assert.EndsWith("Z", receipt.GetProperty("paidAt").GetString());

        var second = await PayAsync(number, code);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);

        var after = await (await LookupAsync(number, code)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(after.GetProperty("isPaid").GetBoolean());
    }

    [Fact]
    public async Task A_wrong_code_does_not_pay_the_invoice()
    {
        var invoice = await AdminCreateAsync(InvoiceBody());
        var number = invoice.GetProperty("invoiceNumber").GetString();
        var code = invoice.GetProperty("paymentCode").GetString();

        var wrong = await PayAsync(number, WrongCodeFor(code));

        Assert.Equal(HttpStatusCode.NotFound, wrong.StatusCode);
        var after = await (await LookupAsync(number, code)).Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(after.GetProperty("isPaid").GetBoolean());
    }

    [Fact]
    public async Task Member_sees_their_own_invoices_with_codes_and_pays_them_once()
    {
        var member = await TestHelpers.RegisterMemberAsync(_client);
        var profile = await (await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, $"/api/users/{member.Username}", member)))
            .Content.ReadFromJsonAsync<JsonElement>();
        var memberId = profile.GetProperty("id").GetInt32();
        var invoice = await AdminCreateAsync(InvoiceBody(userId: memberId));
        var number = invoice.GetProperty("invoiceNumber").GetString();

        var mine = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/invoices/mine", member));
        var rows = await mine.Content.ReadFromJsonAsync<JsonElement>();
        var row = rows.EnumerateArray().Single(r => r.GetProperty("invoiceNumber").GetString() == number);
        Assert.Equal(invoice.GetProperty("paymentCode").GetString(), row.GetProperty("paymentCode").GetString());

        var pay = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, $"/api/invoices/{number}/pay", member));
        Assert.Equal(HttpStatusCode.OK, pay.StatusCode);
        var again = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, $"/api/invoices/{number}/pay", member));
        Assert.Equal(HttpStatusCode.Conflict, again.StatusCode);
    }

    [Fact]
    public async Task Admin_cannot_create_an_invoice_for_a_user_that_does_not_exist()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/invoices", admin,
            InvoiceBody(userId: 999999)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(100001.0)]
    public async Task Amount_outside_0_01_to_100000_is_400(double amount)
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Post, "/api/admin/invoices", admin,
            InvoiceBody(amount: (decimal)amount)));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_status_filter_is_400()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);

        var response = await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/admin/invoices?status=lost", admin));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Summary_counts_a_new_overdue_invoice_as_unpaid_and_overdue()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var before = await (await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/admin/invoices/summary", admin)))
            .Content.ReadFromJsonAsync<JsonElement>();

        await AdminCreateAsync(InvoiceBody(amount: 50m, dueDate: DateTime.UtcNow.AddDays(-5)));

        var after = await (await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/admin/invoices/summary", admin)))
            .Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(before.GetProperty("unpaidCount").GetInt32() + 1, after.GetProperty("unpaidCount").GetInt32());
        Assert.Equal(before.GetProperty("overdueCount").GetInt32() + 1, after.GetProperty("overdueCount").GetInt32());
        Assert.Equal(before.GetProperty("unpaidTotal").GetDecimal() + 50m, after.GetProperty("unpaidTotal").GetDecimal());
    }

    [Fact]
    public async Task Seeded_invoices_are_payable_and_the_seed_has_paid_and_unpaid_bills()
    {
        var admin = await TestHelpers.LoginAdminAsync(_client);
        var list = await (await _client.SendAsync(TestHelpers.Request(HttpMethod.Get, "/api/admin/invoices", admin)))
            .Content.ReadFromJsonAsync<JsonElement>();

        Assert.Contains(list.EnumerateArray(), i => i.GetProperty("isPaid").GetBoolean());
        Assert.Contains(list.EnumerateArray(), i => !i.GetProperty("isPaid").GetBoolean());
        Assert.All(list.EnumerateArray(), i => Assert.NotNull(i.GetProperty("dueDate").GetString()));
    }
}
