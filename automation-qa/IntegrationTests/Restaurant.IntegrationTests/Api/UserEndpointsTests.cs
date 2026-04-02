using FluentAssertions;
using Restaurant.Api.Tests;
using Restaurant.Core.Errors;
using System.Net;
using System.Net.Http.Headers;
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

    [Fact]
    public async Task GetMe_WithoutUserHeader_ShouldReturn401()
    {
        _factory.UserService.Reset();

        var res = await _client.GetAsync("/user/me");

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.UserService.LastGetMeUserId.Should().BeNull();
    }

    [Fact]
    public async Task GetMe_ShouldReturn200_AndReturnMappedUser()
    {
        _factory.UserService.Reset();

        var req = Authed(HttpMethod.Get, "/user/me", userId: "user-42");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.UserService.LastGetMeUserId.Should().Be("user-42");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        root.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
        var data = root.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("userId").GetString().Should().Be("user-1");
        data.GetPropertyIgnoreCase("email").GetString().Should().Be("john.doe@test.com");
        data.GetPropertyIgnoreCase("imageUrl").GetString().Should().Be("https://cdn.test/avatar.jpg");
        data.GetPropertyIgnoreCase("feedbacksCount").GetInt32().Should().Be(2);
        data.GetPropertyIgnoreCase("rating").GetDouble().Should().Be(3.5d);
    }

    [Fact]
    public async Task GetMe_WhenUserNotFound_ShouldReturn404()
    {
        _factory.UserService.Reset();
        _factory.UserService.GetMeFailResult = UserErrors.UserNotFound;

        var req = Authed(HttpMethod.Get, "/user/me", userId: "ghost-user");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAvatar_WithoutUserHeader_ShouldReturn401()
    {
        _factory.UserService.Reset();

        using var content = CreateAvatarMultipartContent("image/png", [0x89, 0x50, 0x4E, 0x47]);
        var res = await _client.PostAsync("/user/avatar", content);

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.UserService.LastUpdateAvatarUserId.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAvatar_ShouldReturn200_AndCallServiceWithFileMetadata()
    {
        _factory.UserService.Reset();

        var req = Authed(HttpMethod.Post, "/user/avatar", userId: "user-77");
        req.Content = CreateAvatarMultipartContent("image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D]);

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.UserService.LastUpdateAvatarUserId.Should().Be("user-77");
        _factory.UserService.LastAvatarContentType.Should().Be("image/png");
        _factory.UserService.LastAvatarSize.Should().Be(5);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var root = doc.RootElement;
        root.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
        root.GetPropertyIgnoreCase("data").GetString().Should().Be("https://cdn.test/avatar.jpg");
    }

    [Fact]
    public async Task UpdateAvatar_WhenInvalidType_ShouldReturn400()
    {
        _factory.UserService.Reset();
        _factory.UserService.UpdateAvatarFailResult = FileErrors.InvalidFileType;

        var req = Authed(HttpMethod.Post, "/user/avatar", userId: "user-77");
        req.Content = CreateAvatarMultipartContent("application/pdf", [0x25, 0x50, 0x44, 0x46]);

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAvatar_WhenFileTooBig_ShouldReturn400()
    {
        _factory.UserService.Reset();
        _factory.UserService.UpdateAvatarFailResult = FileErrors.FileTooBig;

        var req = Authed(HttpMethod.Post, "/user/avatar", userId: "user-77");
        req.Content = CreateAvatarMultipartContent("image/png", [0x89, 0x50, 0x4E, 0x47]);

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateAvatar_WhenUserNotFound_ShouldReturn404()
    {
        _factory.UserService.Reset();
        _factory.UserService.UpdateAvatarFailResult = UserErrors.UserNotFound;

        var req = Authed(HttpMethod.Post, "/user/avatar", userId: "ghost-user");
        req.Content = CreateAvatarMultipartContent("image/png", [0x89, 0x50, 0x4E, 0x47]);

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UpdateAvatar_WhenStorageFails_ShouldReturn500()
    {
        _factory.UserService.Reset();
        _factory.UserService.UpdateAvatarExceptionToThrow = new Exception("Failed to upload to file system");

        var req = Authed(HttpMethod.Post, "/user/avatar", userId: "user-77");
        req.Content = CreateAvatarMultipartContent("image/png", [0x89, 0x50, 0x4E, 0x47]);

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.InternalServerError);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString().Should().Be("Failed to upload to file system");
    }
    
    [Fact]
    public async Task UpdateUsername_WithoutUserHeader_ShouldReturn401()
    {
        _factory.UserService.Reset();

        var res = await _client.PutAsync("/user/username",
            Json(new { firstName = "John", lastName = "Doe" }));

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _factory.UserService.LastUpdateUserNameUserId.Should().BeNull();
    }
    
    [Fact]
    public async Task UpdateUsername_ShouldReturn200_AndCallServiceWithCorrectArgs()
    {
        _factory.UserService.Reset();

        var req = Authed(HttpMethod.Put, "/user/username", userId: "user-42");
        req.Content = Json(new { firstName = "John", lastName = "Doe" });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        var root = doc.RootElement;

        root.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
        root.GetPropertyIgnoreCase("message").GetString()
            .Should().Be("Username updated successfully");

        _factory.UserService.LastUpdateUserNameUserId.Should().Be("user-42");
        _factory.UserService.LastUpdateFirstName.Should().Be("John");
        _factory.UserService.LastUpdateLastName.Should().Be("Doe");
    }
    
    [Fact]
    public async Task UpdateUsername_WhenValidationFails_ShouldReturn400_AndNotCallService()
    {
        _factory.UserService.Reset();

        var req = Authed(HttpMethod.Put, "/user/username", userId: "user-42");
        req.Content = Json(new { firstName = "", lastName = "" }); // invalid

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        _factory.UserService.LastUpdateUserNameUserId.Should().BeNull();
    }
    
    [Fact]
    public async Task UpdateUsername_WhenUserNotFound_ShouldReturn404()
    {
        _factory.UserService.Reset();
        _factory.UserService.UpdateUserNameFailResult = UserErrors.UserNotFound;

        var req = Authed(HttpMethod.Put, "/user/username", userId: "ghost-user");
        req.Content = Json(new { firstName = "John", lastName = "Doe" });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
    }
    
    [Fact]
    public async Task UpdateUsername_WhenValidationErrorFromService_ShouldReturn400()
    {
        _factory.UserService.Reset();
        _factory.UserService.UpdateUserNameFailResult = UserErrors.UpdateNotSuccessful;

        var req = Authed(HttpMethod.Put, "/user/username", userId: "user-42");
        req.Content = Json(new { firstName = "J", lastName = "D" });

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static MultipartFormDataContent CreateAvatarMultipartContent(string contentType, byte[] payload)
    {
        var multipart = new MultipartFormDataContent();
        var file = new ByteArrayContent(payload);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        multipart.Add(file, "file", "avatar.bin");
        return multipart;
    }
}
