namespace Restaurant.Api.Contracts.Responses;

public sealed class WaiterCustomerLookupResponse
{
    public string CustomerId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string MaskedEmail { get; set; } = null!;
}
