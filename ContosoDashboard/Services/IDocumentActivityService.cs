using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDocumentActivityService
{
    Task LogActivityAsync(int documentId, int actorUserId, DocumentActivityType activityType, object? details = null, CancellationToken cancellationToken = default);

    Task<DocumentActivitySummaryResponse> GetActivitySummaryAsync(int requestingUserId, CancellationToken cancellationToken = default);
}