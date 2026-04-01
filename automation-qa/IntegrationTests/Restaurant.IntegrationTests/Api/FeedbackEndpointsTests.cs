using FluentAssertions;
using Restaurant.Api.Tests;
using Restaurant.Core.DTOs;
using FluentResults;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Restaurant.Core.Errors;

namespace Restaurant.IntegrationTests.Api;

public sealed class FeedbackEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public FeedbackEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }

    private static HttpRequestMessage Authed(HttpMethod method, string url, string userId = "customer-1", string? role = null)
    {
        var req = new HttpRequestMessage(method, url);
        req.Headers.Add("X-User-Id", userId);
        if (!string.IsNullOrWhiteSpace(role))
            req.Headers.Add("X-Role", role);
        return req;
    }
    
    [Fact]
    public async Task CreateFeedbackAuthorised_WhenValid_ShouldReturn200()
    {
        _factory.FeedbackService.Reset();
        _factory.FeedbackService.SaveAuthorisedFeedbackResponse = Result.Ok();

        var dto = new CreateFeedbackDTO
        {
            ReservationId = "rsv-001",
            ServiceRating = 5,
            ServiceComment = "Great service",
            CuisineRating = 4,
            CuisineComment = "Tasty food"
        };

        var request = Authed(HttpMethod.Post, "/feedbacks/authorised", userId: "customer-1");
        request.Content = JsonContent.Create(dto);

        var res = await _client.SendAsync(request);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.FeedbackService.LastAuthorisedDto!.ReservationId.Should().Be("rsv-001");
        _factory.FeedbackService.LastAuthorisedUserId.Should().NotBeNullOrEmpty();

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateFeedbackAuthorised_WithServiceRatingOnly_ShouldReturn200()
    {
        _factory.FeedbackService.Reset();
        _factory.FeedbackService.SaveAuthorisedFeedbackResponse = Result.Ok();

        var dto = new CreateFeedbackDTO
        {
            ReservationId = "rsv-002",
            ServiceRating = 4,
        };

        var request = Authed(HttpMethod.Post, "/feedbacks/authorised", userId: "customer-1");
        request.Content = JsonContent.Create(dto);

        var res = await _client.SendAsync(request);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.FeedbackService.LastAuthorisedDto!.ReservationId.Should().Be("rsv-002");
        _factory.FeedbackService.LastAuthorisedDto.CuisineRating.Should().BeNull();
    }

    [Fact]
    public async Task CreateFeedbackAuthorised_WithCuisineRatingOnly_ShouldReturn200()
    {
        _factory.FeedbackService.Reset();
        _factory.FeedbackService.SaveAuthorisedFeedbackResponse = Result.Ok();

        var dto = new CreateFeedbackDTO
        {
            ReservationId = "rsv-003",
            CuisineRating = 3,
            CuisineComment = "Average"
        };

        var request = Authed(HttpMethod.Post, "/feedbacks/authorised", userId: "customer-1");
        request.Content = JsonContent.Create(dto);

        var res = await _client.SendAsync(request);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.FeedbackService.LastAuthorisedDto!.ServiceRating.Should().BeNull();
        _factory.FeedbackService.LastAuthorisedDto.CuisineRating.Should().Be(3);
    }

    [Fact]
    public async Task CreateFeedbackAuthorised_WhenServiceFails_ShouldReturnFailResponse()
    {
        _factory.FeedbackService.Reset();
        _factory.FeedbackService.SaveAuthorisedFeedbackResponse =
            FeedbackErrors.FeedbackAlreadyMade;

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-dup" };

        var request = Authed(HttpMethod.Post, "/feedbacks/authorised", userId: "customer-1");
        request.Content = JsonContent.Create(dto);

        var res = await _client.SendAsync(request);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());

        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();

        doc.RootElement.GetPropertyIgnoreCase("message").GetString()
            .Should().Be(FeedbackErrors.FeedbackAlreadyMade.Message);
    }


    [Fact]
    public async Task CreateFeedbackVisitor_WhenValid_ShouldReturn200()
    {
        _factory.FeedbackService.Reset();
        _factory.FeedbackService.SaveVisitorFeedbackResponse = Result.Ok();

        var dto = new CreateFeedbackDTO
        {
            ReservationId  = "rsv-vis-001",
            ServiceRating  = 5,
            CuisineRating  = 5,
        };

        var res = await _client.PostAsJsonAsync("/feedbacks/visitor?secretCode=ALPHA-7X", dto);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.FeedbackService.LastVisitorDto!.ReservationId.Should().Be("rsv-vis-001");
        _factory.FeedbackService.LastVisitorSecretCode.Should().Be("ALPHA-7X");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task CreateFeedbackVisitor_WhenWrongSecretCode_ShouldReturnFailResponse()
    {
        _factory.FeedbackService.Reset();
        _factory.FeedbackService.SaveVisitorFeedbackResponse =
            Result.Fail(FeedbackErrors.ReservationUnauthorizedAccess);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-vis-002" };

        var res = await _client.PostAsJsonAsync("/feedbacks/visitor?secretCode=WRONG", dto);

        _factory.FeedbackService.LastVisitorSecretCode.Should().Be("WRONG");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString()
            .Should().Be(FeedbackErrors.ReservationUnauthorizedAccess.Message);
    }

    [Fact]
    public async Task CreateFeedbackVisitor_WhenReservationNotFound_ShouldReturnFailResponse()
    {
        _factory.FeedbackService.Reset();
        _factory.FeedbackService.SaveVisitorFeedbackResponse =
            Result.Fail(ReservationErrors.ReservationNotFound);

        var dto = new CreateFeedbackDTO { ReservationId = "rsv-missing" };

        var res = await _client.PostAsJsonAsync("/feedbacks/visitor?secretCode=BRAVO-2K", dto);

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString()
            .Should().Be(ReservationErrors.ReservationNotFound.Message);
    }


    [Fact]
    public async Task GetOverallFeedbackData_WhenValid_ShouldReturn200_AndMapAllFields()
    {
        _factory.FeedbackService.Reset();
        _factory.FeedbackService.GetCalculatedFeedbackDataResponse = Result.Ok(new WaiterLocationFeedbackDTO
        {
            WaiterName           = "Giorgi Beridze",
            WaiterImageUrl       = "http://img/waiter-1",
            WaiterRating         = 4.8,
            WaiterFeedbacksNumber = 48,
            CuisineRating        = 4.5,
            CuisineFeedbacksNumber = 90
        });
        var request = Authed(HttpMethod.Get, "/feedbacks/feedback-short-data?reservationId=rsv-001");
        var res = await _client.SendAsync(request);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        _factory.FeedbackService.LastCalculatedReservationId.Should().Be("rsv-001");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

        var data = doc.RootElement.GetPropertyIgnoreCase("data");
        data.GetPropertyIgnoreCase("waiterName").GetString().Should().Be("Giorgi Beridze");
        data.GetPropertyIgnoreCase("waiterImageUrl").GetString().Should().Be("http://img/waiter-1");
        data.GetPropertyIgnoreCase("waiterRating").GetDouble().Should().BeApproximately(4.8, 0.001);
        data.GetPropertyIgnoreCase("waiterFeedbacksNumber").GetInt32().Should().Be(48);
        data.GetPropertyIgnoreCase("cuisineRating").GetDouble().Should().BeApproximately(4.5, 0.001);
        data.GetPropertyIgnoreCase("cuisineFeedbacksNumber").GetInt32().Should().Be(90);
    }

    [Fact]
    public async Task GetOverallFeedbackData_WhenServiceFails_ShouldReturnFailResponse()
    {
        _factory.FeedbackService.Reset();
        _factory.FeedbackService.GetCalculatedFeedbackDataResponse =
            Result.Fail<WaiterLocationFeedbackDTO>(FeedbackErrors.DataFetchingError);

        var request = Authed(HttpMethod.Get, "/feedbacks/feedback-short-data?reservationId=rsv-bad");
        var res = await _client.SendAsync(request);

        _factory.FeedbackService.LastCalculatedReservationId.Should().Be("rsv-bad");

        using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
        doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
        doc.RootElement.GetPropertyIgnoreCase("message").GetString()
            .Should().Be(FeedbackErrors.DataFetchingError.Message);
    }

    [Fact]
    public async Task GetOverallFeedbackData_WhenMissingReservationId_ShouldReturn400()
    {
        _factory.FeedbackService.Reset();

        var request = Authed(HttpMethod.Get, "/feedbacks/feedback-short-data");
        var res = await _client.SendAsync(request);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        _factory.FeedbackService.LastCalculatedReservationId.Should().BeNull();
    }
    
    [Fact]
public async Task GetUpdateFeedbackData_WhenValid_ShouldReturn200_AndMapAllFields()
{
    _factory.FeedbackService.Reset();
    _factory.FeedbackService.GetCalculatedFeedbackDataResponse = Result.Ok(new WaiterLocationFeedbackDTO
    {
        WaiterName             = "Giorgi Beridze",
        WaiterImageUrl         = "http://img/waiter-1",
        WaiterRating           = 4.8,
        WaiterFeedbacksNumber  = 48,
        CuisineRating          = 4.5,
        CuisineFeedbacksNumber = 90,
        UpdateUserData = new FeedbackOfUserDTO
        {
            KitchenFeedbackId = "kf-001",
            KitchenRating     = 4,
            KitchenComment    = "Good food",
            ServiceFeedbackId = "sf-001",
            ServiceRating     = 5,
            ServiceComment    = "Great service"
        }
    });

    var request = Authed(HttpMethod.Get, "/feedbacks/feedback-short-update-data?reservationId=rsv-001");
    var res = await _client.SendAsync(request);

    res.StatusCode.Should().Be(HttpStatusCode.OK);
    _factory.FeedbackService.LastCalculatedReservationId.Should().Be("rsv-001");

    using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
    doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();

    var data = doc.RootElement.GetPropertyIgnoreCase("data");
    data.GetPropertyIgnoreCase("waiterName").GetString().Should().Be("Giorgi Beridze");
    data.GetPropertyIgnoreCase("waiterRating").GetDouble().Should().BeApproximately(4.8, 0.001);

    var updateData = data.GetPropertyIgnoreCase("updateUserData");
    updateData.GetPropertyIgnoreCase("kitchenFeedbackId").GetString().Should().Be("kf-001");
    updateData.GetPropertyIgnoreCase("kitchenRating").GetInt32().Should().Be(4);
    updateData.GetPropertyIgnoreCase("serviceFeedbackId").GetString().Should().Be("sf-001");
    updateData.GetPropertyIgnoreCase("serviceRating").GetInt32().Should().Be(5);
}

[Fact]
public async Task GetUpdateFeedbackData_WhenServiceFails_ShouldReturnFailResponse()
{
    _factory.FeedbackService.Reset();
    _factory.FeedbackService.GetCalculatedFeedbackDataResponse =
        Result.Fail<WaiterLocationFeedbackDTO>(FeedbackErrors.DataFetchingError);

    var request = Authed(HttpMethod.Get, "/feedbacks/feedback-short-update-data?reservationId=rsv-bad");
    var res = await _client.SendAsync(request);

    _factory.FeedbackService.LastCalculatedReservationId.Should().Be("rsv-bad");

    using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
    doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
    doc.RootElement.GetPropertyIgnoreCase("message").GetString()
        .Should().Be(FeedbackErrors.DataFetchingError.Message);
}

[Fact]
public async Task GetUpdateFeedbackData_WhenMissingReservationId_ShouldReturn400()
{
    _factory.FeedbackService.Reset();

    var request = Authed(HttpMethod.Get, "/feedbacks/feedback-short-update-data");
    var res = await _client.SendAsync(request);

    res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    _factory.FeedbackService.LastCalculatedReservationId.Should().BeNull();
}

[Fact]
public async Task UpdateFeedback_WhenValid_ShouldReturn200()
{
    _factory.FeedbackService.Reset();
    _factory.FeedbackService.UpdateFeedbackResponse = Result.Ok();

    var dto = new CreateFeedbackDTO
    {
        ReservationId  = "rsv-001",
        ServiceRating  = 4,
        ServiceComment = "Updated comment",
        CuisineRating  = 5,
        CuisineComment = "Even better food"
    };

    var request = Authed(HttpMethod.Put, "/feedbacks/update-feedback", userId: "customer-1");
    request.Content = JsonContent.Create(dto);

    var res = await _client.SendAsync(request);

    res.StatusCode.Should().Be(HttpStatusCode.OK);
    _factory.FeedbackService.LastUpdateDto!.ReservationId.Should().Be("rsv-001");
    _factory.FeedbackService.LastUpdateUserId.Should().NotBeNullOrEmpty();

    using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
    doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeTrue();
}

[Fact]
public async Task UpdateFeedback_WhenServiceFails_ShouldReturnFailResponse()
{
    _factory.FeedbackService.Reset();
    _factory.FeedbackService.UpdateFeedbackResponse = FeedbackErrors.FeedbackUpdateUnsuccessful;

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-001" };

    var request = Authed(HttpMethod.Put, "/feedbacks/update-feedback", userId: "customer-1");
    request.Content = JsonContent.Create(dto);

    var res = await _client.SendAsync(request);

    using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
    doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
    doc.RootElement.GetPropertyIgnoreCase("message").GetString()
        .Should().Be(FeedbackErrors.FeedbackUpdateUnsuccessful.Message);
}

[Fact]
public async Task UpdateFeedback_WhenFeedbackNotFound_ShouldReturnFailResponse()
{
    _factory.FeedbackService.Reset();
    _factory.FeedbackService.UpdateFeedbackResponse = FeedbackErrors.FeedbackNotFound;

    var dto = new CreateFeedbackDTO { ReservationId = "rsv-missing" };

    var request = Authed(HttpMethod.Put, "/feedbacks/update-feedback", userId: "customer-1");
    request.Content = JsonContent.Create(dto);

    var res = await _client.SendAsync(request);

    using var doc = JsonDocument.Parse(await res.Content.ReadAsStringAsync());
    doc.RootElement.GetPropertyIgnoreCase("isSuccess").GetBoolean().Should().BeFalse();
    doc.RootElement.GetPropertyIgnoreCase("message").GetString()
        .Should().Be(FeedbackErrors.FeedbackNotFound.Message);
}
}