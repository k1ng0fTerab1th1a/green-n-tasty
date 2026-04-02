namespace Restaurant.IntegrationTests.Api;

using System.Net;
using FluentAssertions;
using Restaurant.Core.Errors;

public class ReceiptEndpointsTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ReceiptEndpointsTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client  = factory.CreateClient();
    }


    [Fact]
    public async Task GetReceipt_WithoutAuthHeader_ShouldReturn401()
    {
        _factory.ReceiptService.Reset();

        var res = await _client.GetAsync("/reservations/r-1/receipt");

        res.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        _factory.ReceiptService.LastReservationId.Should().BeNull();
    }

    [Fact]
    public async Task GetReceipt_AsCustomer_ShouldReturn403()
    {
        _factory.ReceiptService.Reset();

        var req = new HttpRequestMessage(HttpMethod.Get, "/reservations/r-1/receipt");
        req.Headers.Add("X-User-Id", "customer-1");
        req.Headers.Add("X-Role",    "CUSTOMER");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        _factory.ReceiptService.LastReservationId.Should().BeNull();
    }


    [Fact]
    public async Task GetReceipt_AsWaiter_WhenSuccess_ShouldReturn200_WithPdfContent()
    {
        _factory.ReceiptService.Reset();
        var pdfBytes = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }; // %PDF-
        _factory.ReceiptService.PdfResponse = pdfBytes;

        var req = new HttpRequestMessage(HttpMethod.Get, "/reservations/res-42/receipt");
        req.Headers.Add("X-User-Id", "waiter-1");
        req.Headers.Add("X-Role",    "WAITER");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.OK);

        res.Content.Headers.ContentType!.MediaType
            .Should().Be("application/pdf");

        var fileName = res.Content.Headers.ContentDisposition!.FileNameStar
                       ?? res.Content.Headers.ContentDisposition.FileName;

        fileName.Should().Contain("receipt-res-42.pdf");

        var body = await res.Content.ReadAsByteArrayAsync();
        body.Should().Equal(pdfBytes);

        _factory.ReceiptService.LastReservationId.Should().Be("res-42");
        _factory.ReceiptService.LastWaiterId.Should().Be("waiter-1");
    }

    [Fact]
    public async Task GetReceipt_AsWaiter_ShouldPassCorrectWaiterIdFromClaims()
    {
        _factory.ReceiptService.Reset();

        var req = new HttpRequestMessage(HttpMethod.Get, "/reservations/r-99/receipt");
        req.Headers.Add("X-User-Id", "waiter-7");
        req.Headers.Add("X-Role",    "WAITER");

        await _client.SendAsync(req);

        _factory.ReceiptService.LastWaiterId.Should().Be("waiter-7");
        _factory.ReceiptService.LastReservationId.Should().Be("r-99");
    }

    // ── error paths ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetReceipt_AsWaiter_WhenReservationNotFound_ShouldReturn404()
    {
        _factory.ReceiptService.Reset();
        _factory.ReceiptService.FailResult = ReservationErrors.ReservationNotFound;

        var req = new HttpRequestMessage(HttpMethod.Get, "/reservations/ghost-id/receipt");
        req.Headers.Add("X-User-Id", "waiter-1");
        req.Headers.Add("X-Role",    "WAITER");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        _factory.ReceiptService.LastReservationId.Should().Be("ghost-id");
    }

    [Fact]
    public async Task GetReceipt_AsWaiter_WhenNotOwnerOfReservation_ShouldReturn403()
    {
        _factory.ReceiptService.Reset();
        _factory.ReceiptService.FailResult = ReceiptErrors.Forbidden;

        var req = new HttpRequestMessage(HttpMethod.Get, "/reservations/r-1/receipt");
        req.Headers.Add("X-User-Id", "waiter-99");
        req.Headers.Add("X-Role",    "WAITER");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GetReceipt_AsWaiter_WhenReservationNotFinished_ShouldReturn400()
    {
        _factory.ReceiptService.Reset();
        _factory.ReceiptService.FailResult = ReceiptErrors.ReservationNotFinished;

        var req = new HttpRequestMessage(HttpMethod.Get, "/reservations/r-1/receipt");
        req.Headers.Add("X-User-Id", "waiter-1");
        req.Headers.Add("X-Role",    "WAITER");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetReceipt_AsWaiter_WhenOrderNotFound_ShouldReturn404()
    {
        _factory.ReceiptService.Reset();
        _factory.ReceiptService.FailResult = ReceiptErrors.OrderNotFound;

        var req = new HttpRequestMessage(HttpMethod.Get, "/reservations/r-1/receipt");
        req.Headers.Add("X-User-Id", "waiter-1");
        req.Headers.Add("X-Role",    "WAITER");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetReceipt_AsWaiter_WhenVisitorSecretCodeMissing_ShouldReturn400()
    {
        _factory.ReceiptService.Reset();
        _factory.ReceiptService.FailResult = ReceiptErrors.VisitorSecretCodeMissing;

        var req = new HttpRequestMessage(HttpMethod.Get, "/reservations/r-visitor/receipt");
        req.Headers.Add("X-User-Id", "waiter-1");
        req.Headers.Add("X-Role",    "WAITER");

        var res = await _client.SendAsync(req);

        res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}