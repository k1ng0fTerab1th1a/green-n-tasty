using FluentResults;
using Restaurant.Core.DTOs;

namespace Restaurant.Core.Interfaces.Services;

public interface IFeedbackService
{
    Task<Result<FeedbackPaginatedDto>> GetFeedbacksForLocation(string locationId, int size, string type,
        List<string> sort, string? pageToken, CancellationToken ct);

    Task<Result> SaveAuthorisedFeedback(CreateFeedbackDTO dto, string userId, CancellationToken ct);
    Task<Result> SaveVisitorFeedback(CreateFeedbackDTO dto, string secretCode, CancellationToken ct);

    Task<Result<WaiterLocationFeedbackDTO>> GetWaiterLocationFeedbackDTOAsync(string reservationId, bool isForUpdate,
        CancellationToken ct);

    Task<Result<byte[]>> GenerateFeedbackQr(string reservationId, CancellationToken ct);

    Task<Result> UpdateFeedback(string feedbackId, string comment, int rating, string feedbackType,
        CancellationToken ct);
}