namespace Restaurant.Core.DTOs;

public sealed record WaiterCustomerLookupDTO(
    string CustomerId,
    string Username,
    string MaskedEmail
);
