using FluentResults;
using Restaurant.Core.DTOs;

namespace Restaurant.Core.Interfaces.Services;

public interface IReceiptPdfService
{
    Task<Result<byte[]>> GeneratePdfAsync(ReceiptDTO receipt, CancellationToken ct);
}
