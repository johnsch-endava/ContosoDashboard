using Microsoft.EntityFrameworkCore;
using ContosoDashboard.Data;
using ContosoDashboard.Models;

namespace ContosoDashboard.Services;

public interface IDashboardService
{
    Task<DashboardSummary> GetDashboardSummaryAsync(int userId);
    Task<List<Announcement>> GetActiveAnnouncementsAsync();
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummary> GetDashboardSummaryAsync(int userId)
    {
        var now = DateTime.UtcNow;

        var summary = new DashboardSummary
        {
            TotalActiveTasks = await _context.Tasks
                .CountAsync(t => t.AssignedUserId == userId && t.Status != Models.TaskStatus.Completed),

            TasksDueToday = await _context.Tasks
                .CountAsync(t => t.AssignedUserId == userId 
                    && t.DueDate.HasValue 
                    && t.DueDate.Value.Date == now.Date
                    && t.Status != Models.TaskStatus.Completed),

            ActiveProjects = await _context.Projects
                .Where(p => p.ProjectManagerId == userId || p.ProjectMembers.Any(pm => pm.UserId == userId))
                .Where(p => p.Status == ProjectStatus.Active)
                .CountAsync(),

            TotalDocuments = await _context.Documents
                .CountAsync(document => document.UploadedByUserId == userId && document.DeletedUtc == null),

            UnreadNotifications = await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead)
        };

        summary.RecentDocuments = await _context.Documents
            .Where(document => document.UploadedByUserId == userId && document.DeletedUtc == null)
            .OrderByDescending(document => document.CreatedUtc)
            .Take(5)
            .Select(document => new DashboardRecentDocument
            {
                DocumentId = document.DocumentId,
                Title = document.Title,
                Category = document.Category,
                CreatedUtc = document.CreatedUtc,
                ScanStatus = document.ScanStatus
            })
            .ToListAsync();

        return summary;
    }

    public async Task<List<Announcement>> GetActiveAnnouncementsAsync()
    {
        var now = DateTime.UtcNow;

        return await _context.Announcements
            .Include(a => a.CreatedByUser)
            .Where(a => a.IsActive 
                && a.PublishDate <= now 
                && (!a.ExpiryDate.HasValue || a.ExpiryDate.Value > now))
            .OrderByDescending(a => a.PublishDate)
            .Take(5)
            .ToListAsync();
    }
}

public class DashboardSummary
{
    public int TotalActiveTasks { get; set; }
    public int TasksDueToday { get; set; }
    public int ActiveProjects { get; set; }
    public int TotalDocuments { get; set; }
    public int UnreadNotifications { get; set; }
    public List<DashboardRecentDocument> RecentDocuments { get; set; } = new();
}

public class DashboardRecentDocument
{
    public int DocumentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; }
    public DocumentScanStatus ScanStatus { get; set; }
}
