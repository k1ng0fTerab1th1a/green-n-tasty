using FluentResults;

namespace Restaurant.Core.Interfaces.Services;

public interface IReceiptService
{
    Task<Result<byte[]>> GetReceiptAsync(string reservationId, string waiterId, CancellationToken ct);
}
