namespace Restaurant.Core.DTOs;

public sealed class WaiterCustomerLookupDTO
{
    public WaiterCustomerLookupDTO(string customerId, string username, string maskedEmail)
    {
        CustomerId = customerId;
        Username = username;
        MaskedEmail = maskedEmail;
    }

    public string CustomerId { get; set; } = null!;
    public string Username { get; set; } = null!;
    public string MaskedEmail { get; set; } = null!;
}