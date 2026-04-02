using System.Net;
using System.Text.Json;
using FluentAssertions;
using Restaurant.Api.Tests;
using Restaurant.Reports.Domain.Errors;

namespace Restaurant.IntegrationTests.Api;

public sealed class ReportEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ReportEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }
    
    private static HttpRequestMessage Admin(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("X-User-Id", "admin-1");
        req.Headers.Add("X-Role", "ADMIN");
        return req;
    }
    
    [Fact]
    public async Task GetLocationReport_WithoutAuth_ShouldReturn401()
    {
        _factory.ReportService.Reset();

        var res = await _client.GetAsync("/reports/locations?from=2024-01-01&to=2024-01-10");

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    } 
    
    [Fact]
    public async Task GetLocationReport_ShouldReturn200_AndCallService()
    {
        _factory.ReportService.Reset();

        var req = Admin(HttpMethod.Get, "/reports/locations?from=2024-01-01&to=2024-01-10");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        _factory.ReportService.LastRange!.From.Should().Be(
            new DateOnly(2024, 1, 1).ToDateTime(TimeOnly.MinValue)
        );

        _factory.ReportService.LastRange!.To.Should().Be(
            new DateOnly(2024, 1, 10).ToDateTime(TimeOnly.MinValue)
        );

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
    }
    
    [Fact]
    public async Task GetLocationReport_InvalidRange_ShouldReturn400()
    {
        _factory.ReportService.Reset();
        _factory.ReportService.FailResult = ReportErrors.InvalidDateRange;

        var req = Admin(HttpMethod.Get, "/reports/locations?from=2024-02-01&to=2024-01-01");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
    
    [Fact]
    public async Task GetWaiterReport_ShouldReturn200()
    {
        _factory.ReportService.Reset();

        var req = Admin(HttpMethod.Get, "/reports/waiters?from=2024-01-01&to=2024-01-10");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
    }
    
    [Fact]
    public async Task ExportLocationReport_ShouldReturnFile()
    {
        _factory.ReportService.Reset();

        var req = Admin(HttpMethod.Get, "/reports/locations/export?from=2024-01-01&to=2024-01-10");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        res.Content.Headers.ContentType!.MediaType
            .Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        var fileName = res.Content.Headers.ContentDisposition!.FileNameStar
                       ?? res.Content.Headers.ContentDisposition.FileName;

        fileName!.Should().Contain("location-report-2024-01-01_2024-01-10.xlsx");
    }
    
    [Fact]
    public async Task ExportWaiterReport_ShouldReturnFile()
    {
        _factory.ReportService.Reset();

        var req = Admin(HttpMethod.Get, "/reports/waiters/export?from=2024-01-01&to=2024-01-10");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var fileName = res.Content.Headers.ContentDisposition!.FileNameStar
                       ?? res.Content.Headers.ContentDisposition.FileName;

        fileName!.Should().Contain("waiter-report-2024-01-01_2024-01-10.xlsx");
    }
    
    [Fact]
    public async Task ExportLocationReport_WhenFails_ShouldReturn400()
    {
        _factory.ReportService.Reset();
        _factory.ReportService.FailResult = ReportErrors.InvalidDateRange;

        var req = Admin(HttpMethod.Get, "/reports/locations/export?from=2024-02-01&to=2024-01-01");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}