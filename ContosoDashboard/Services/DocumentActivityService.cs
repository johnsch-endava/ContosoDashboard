using System.Text.Json;
using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;

namespace ContosoDashboard.Services;

public class DocumentActivityService : IDocumentActivityService
{
    private readonly ApplicationDbContext _context;

    public DocumentActivityService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogActivityAsync(int documentId, int actorUserId, DocumentActivityType activityType, object? details = null, CancellationToken cancellationToken = default)
    {
        var record = new DocumentActivityRecord
        {
            DocumentId = documentId,
            ActorUserId = actorUserId,
            ActivityType = activityType,
            OccurredUtc = DateTime.UtcNow,
            DetailsJson = details is null ? null : JsonSerializer.Serialize(details)
        };

        _context.DocumentActivityRecords.Add(record);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<DocumentActivitySummaryResponse> GetActivitySummaryAsync(int requestingUserId, CancellationToken cancellationToken = default)
    {
        var requester = await _context.Users.FindAsync([requestingUserId], cancellationToken);
        if (requester?.Role != UserRole.Administrator)
        {
            return new DocumentActivitySummaryResponse();
        }

        var activityByType = await _context.DocumentActivityRecords
            .GroupBy(record => record.ActivityType)
            .Select(group => new DocumentActivitySummaryItem
            {
                ActivityType = group.Key.ToString(),
                Count = group.Count()
            })
            .OrderByDescending(item => item.Count)
            .ToListAsync(cancellationToken);

        var activeUploaders = await _context.DocumentActivityRecords
            .Where(record => record.ActivityType == DocumentActivityType.Upload)
            .GroupBy(record => new { record.ActorUserId, record.ActorUser.DisplayName })
            .Select(group => new DocumentUploaderSummaryItem
            {
                UserId = group.Key.ActorUserId,
                DisplayName = group.Key.DisplayName,
                UploadCount = group.Count()
            })
            .OrderByDescending(item => item.UploadCount)
            .Take(10)
            .ToListAsync(cancellationToken);

        var recentEvents = await _context.DocumentActivityRecords
            .Include(record => record.Document)
            .Include(record => record.ActorUser)
            .OrderByDescending(record => record.OccurredUtc)
            .Take(25)
            .Select(record => new DocumentActivityEventItem
            {
                DocumentId = record.DocumentId,
                DocumentTitle = record.Document.Title,
                ActorDisplayName = record.ActorUser.DisplayName,
                ActivityType = record.ActivityType.ToString(),
                OccurredUtc = record.OccurredUtc
            })
            .ToListAsync(cancellationToken);

        return new DocumentActivitySummaryResponse
        {
            ActivityByType = activityByType,
            ActiveUploaders = activeUploaders,
            RecentEvents = recentEvents
        };
    }
}