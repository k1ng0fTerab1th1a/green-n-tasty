using FluentAssertions;
using Restaurant.Api.Tests;
using Restaurant.Core.Errors;
using System.Net;
using System.Text;
using System.Text.Json;

namespace Restaurant.IntegrationTests.Api;

public sealed class UserEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public UserEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string userId = "user-1")
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("X-User-Id", userId);
        return req;
    }

    private static StringContent Json(object payload) =>
        new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

    [Fact]
    public async Task UpdateEmail_WithoutUserHeader_ShouldReturn401()
    {
        _factory.CognitoService.Reset();

        var res = await _client.PutAsync("/user/email", Json(new { newEmail = "new@test.com" }));

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.CognitoService.LastUpdateEmailArgs.Should().BeNull();
    }

    [Fact]
    public async Task UpdateEmail_ShouldReturn200_AndCallCognitoWithCorrectArgs()
    {
        _factory.CognitoService.Reset();

        var req = Authed(HttpMethod.Put, "/user/email", userId: "user-42");
        req.Content = Json(new { newEmail = "new@test.com" });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Email updated successfully");

        _factory.CognitoService.LastUpdateEmailArgs.Should().Be(("user-42", "new@test.com"));
    }

    [Fact]
    public async Task UpdateEmail_WhenBodyInvalid_ShouldReturn400_AndNotCallCognito()
    {
        _factory.CognitoService.Reset();

        var req = Authed(HttpMethod.Put, "/user/email", userId: "user-42");
        req.Content = Json(new { newEmail = "not-an-email" });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.CognitoService.LastUpdateEmailArgs.Should().BeNull();
    }

    [Fact]
    public async Task UpdateEmail_WhenUserNotFound_ShouldReturn404()
    {
        _factory.CognitoService.Reset();
        _factory.CognitoService.UpdateEmailFailResult = AuthErrors.UserNotFound;

        var req = Authed(HttpMethod.Put, "/user/email", userId: "ghost-user");
        req.Content = Json(new { newEmail = "new@test.com" });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task UpdateEmail_WhenEmailAlreadyTaken_ShouldReturn409()
    {
        _factory.CognitoService.Reset();
        _factory.CognitoService.UpdateEmailFailResult = AuthErrors.UserAlreadyExists;

        var req = Authed(HttpMethod.Put, "/user/email", userId: "user-42");
        req.Content = Json(new { newEmail = "taken@test.com" });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
    }
}
