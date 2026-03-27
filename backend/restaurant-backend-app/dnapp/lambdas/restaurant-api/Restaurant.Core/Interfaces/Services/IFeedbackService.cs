using FluentResults;
using Restaurant.Core.DTOs;

namespace Restaurant.Core.Interfaces.Services;

public interface IFeedbackService
{
    Task<Result<FeedbackPaginatedDto>> GetFeedbacksForLocation(string locationId, int size, string type,
        List<string> sort, string? pageToken = null);

    Task<Result> SaveAuthorisedFeedback(CreateFeedbackDTO dto, string userId, CancellationToken ct = default);
    Task<Result> SaveVisitorFeedback(CreateFeedbackDTO dto, string secretCode, CancellationToken ct = default);

    Task<Result<CalculatedFeedbackDTO>> GetCalculatedFeedbackDataAsync(string reservationId,
        CancellationToken ct = default);
}