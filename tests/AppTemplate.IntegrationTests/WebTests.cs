using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AppTemplate.IntegrationTests;

/// <summary>The standards end to end through HTTP: date format, antiforgery, error responses.</summary>
public sealed partial class WebTests(DatabaseFixture database) : IDisposable
{
    private readonly AppFactory _app = new(database);

    private CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Grid_returns_dates_already_formatted()
    {
        var client = await ClientWithTokenAsync(_app);
        var tag = Unique.Tag();
        await PostFormAsync(client, "/Customers/Create", new() { ["Name"] = tag, ["Email"] = $"{tag}@example.com", ["CustomerSince"] = "05-Mar-2025" });

        var response = await client.PostAsJsonAsync("/Customers/List", GridRequest(tag), Ct);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("05-Mar-2025", json.GetProperty("data")[0].GetProperty("customerSince").GetString());
        Assert.Equal(7, json.GetProperty("draw").GetInt32());
    }

    [Fact]
    public async Task Create_rejects_an_iso_date_with_a_message_naming_the_format()
    {
        var client = await ClientWithTokenAsync(_app);

        var response = await PostFormAsync(client, "/Customers/Create", new() { ["Name"] = "X", ["Email"] = "x@example.com", ["CustomerSince"] = "2025-03-05" });
        var html = await response.Content.ReadAsStringAsync(Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode); // form shown again
        Assert.Contains("Enter Customer since as dd-MMM-yyyy", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Create_refuses_a_date_after_the_business_today()
    {
        var client = await ClientWithTokenAsync(_app);
        var tag = Unique.Tag();

        var response = await PostFormAsync(client, "/Customers/Create", new() { ["Name"] = tag, ["Email"] = $"{tag}@example.com", ["CustomerSince"] = "20-Nov-2026" });

        Assert.Contains("cannot be in the future", await response.Content.ReadAsStringAsync(Ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Edit_form_renders_the_date_as_text_in_house_format()
    {
        var client = await ClientWithTokenAsync(_app);
        var tag = Unique.Tag();
        await PostFormAsync(client, "/Customers/Create", new() { ["Name"] = tag, ["Email"] = $"{tag}@example.com", ["CustomerSince"] = "05-Mar-2025" });
        var grid = await (await client.PostAsJsonAsync("/Customers/List", GridRequest(tag), Ct)).Content.ReadFromJsonAsync<JsonElement>(Ct);
        var id = grid.GetProperty("data")[0].GetProperty("customerId").GetInt32();

        var html = await client.GetStringAsync($"/Customers/Edit/{id}", Ct);

        var input = DateInput().Match(html).Value;
        Assert.Contains("type=\"text\"", input, StringComparison.Ordinal);
        Assert.Contains("value=\"05-Mar-2025\"", input, StringComparison.Ordinal);
        Assert.Contains("data-datepicker", input, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Post_without_antiforgery_token_is_rejected()
    {
        var client = _app.CreateClient();

        var response = await client.PostAsJsonAsync("/Customers/List", GridRequest("x"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Malformed_json_is_a_400_not_a_500()
    {
        var client = await ClientWithTokenAsync(_app);

        var response = await client.PostAsync("/Customers/List", new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"), Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_page_shows_the_404_page()
    {
        var html = await _app.CreateClient().GetAsync("/Customers/Edit/999999999", Ct);

        Assert.Equal(HttpStatusCode.NotFound, html.StatusCode);
        Assert.Contains("does not exist", await html.Content.ReadAsStringAsync(Ct), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unexpected_error_on_ajax_returns_problem_details_without_internals()
    {
        using var broken = new BrokenDatabaseFactory(database);
        var client = await ClientWithTokenAsync(broken);

        var response = await client.PostAsJsonAsync("/Customers/List", GridRequest("x"), Ct);
        var body = await response.Content.ReadAsStringAsync(Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Matches("\"traceId\":\"[0-9a-f]{32}\"", body);
        Assert.DoesNotContain("hunter2", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Unexpected_error_on_a_page_shows_the_error_page_with_a_reference()
    {
        using var broken = new BrokenDatabaseFactory(database);

        var response = await broken.CreateClient().GetAsync("/Customers/Edit/1", Ct);
        var html = await response.Content.ReadAsStringAsync(Ct);

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Matches("<code>[0-9a-f]{32}</code>", html);
        Assert.DoesNotContain("hunter2", html, StringComparison.Ordinal);
    }

    public void Dispose() => _app.Dispose();

    // DataTables' request shape, as site.js sends it.
    private static object GridRequest(string search) => new
    {
        draw = 7,
        start = 0,
        length = 10,
        search = new { value = search },
        order = new[] { new { column = 0, dir = "asc" } },
        columns = new[] { new { data = "name", orderable = true } },
    };

    private async Task<HttpClient> ClientWithTokenAsync(AppFactory app)
    {
        var client = app.CreateClient(); // keeps the antiforgery cookie between requests
        var html = await client.GetStringAsync("/Customers", Ct);
        client.DefaultRequestHeaders.Add("RequestVerificationToken", TokenMeta().Match(html).Groups[1].Value);
        client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
        return client;
    }

    private Task<HttpResponseMessage> PostFormAsync(HttpClient client, string url, Dictionary<string, string> fields) =>
        client.PostAsync(url, new FormUrlEncodedContent(fields), Ct);

    [GeneratedRegex("name=\"request-verification-token\" content=\"([^\"]+)\"")]
    private static partial Regex TokenMeta();

    [GeneratedRegex("<input[^>]*id=\"CustomerSince\"[^>]*>")]
    private static partial Regex DateInput();
}
