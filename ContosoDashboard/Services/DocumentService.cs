using ContosoDashboard.Data;
using ContosoDashboard.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ContosoDashboard.Services;

public class DocumentService : IDocumentService
{
    private static readonly string[] PreviewableExtensions = [".pdf", ".png", ".jpg", ".jpeg"];
    private static readonly string[] AllowedCategories = ["Project Documents", "Team Resources", "Personal Files", "Reports", "Presentations", "Other"];

    private readonly ApplicationDbContext _context;
    private readonly IFileStorageService _fileStorageService;
    private readonly IFileScreeningService _fileScreeningService;
    private readonly IFileScanDispatchService _fileScanDispatchService;
    private readonly IDocumentActivityService _documentActivityService;
    private readonly INotificationService _notificationService;
    private readonly DocumentScanningOptions _documentScanningOptions;

    public DocumentService(
        ApplicationDbContext context,
        IFileStorageService fileStorageService,
        IFileScreeningService fileScreeningService,
        IFileScanDispatchService fileScanDispatchService,
        IDocumentActivityService documentActivityService,
        INotificationService notificationService,
        IOptions<DocumentScanningOptions> documentScanningOptions)
    {
        _context = context;
        _fileStorageService = fileStorageService;
        _fileScreeningService = fileScreeningService;
        _fileScanDispatchService = fileScanDispatchService;
        _documentActivityService = documentActivityService;
        _notificationService = notificationService;
        _documentScanningOptions = documentScanningOptions.Value;
    }

    public async Task<DocumentListResponse> GetDocumentsAsync(int requestingUserId, string? department, DocumentQueryRequest query, CancellationToken cancellationToken = default)
    {
        var documents = await GetDocumentQuery()
            .ToListAsync(cancellationToken);

        IEnumerable<Document> filtered = query.Scope.ToLowerInvariant() switch
        {
            "shared" => documents.Where(document => HasExplicitShare(document, requestingUserId, department) && IsVisible(document, requestingUserId, department)),
            "accessible" => documents.Where(document => CanRead(document, requestingUserId, department) && IsVisible(document, requestingUserId, department)),
            _ => documents.Where(document => document.UploadedByUserId == requestingUserId && document.DeletedUtc is null)
        };

        if (query.ProjectId.HasValue)
        {
            filtered = filtered.Where(document => document.ProjectId == query.ProjectId.Value);
        }

        if (query.TaskId.HasValue)
        {
            filtered = filtered.Where(document => document.TaskId == query.TaskId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Category))
        {
            filtered = filtered.Where(document => string.Equals(document.Category, query.Category, StringComparison.OrdinalIgnoreCase));
        }

        if (query.FromDate.HasValue)
        {
            filtered = filtered.Where(document => document.CreatedUtc.Date >= query.FromDate.Value.Date);
        }

        if (query.ToDate.HasValue)
        {
            filtered = filtered.Where(document => document.CreatedUtc.Date <= query.ToDate.Value.Date);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchTerm = query.Search.Trim();
            filtered = filtered.Where(document => MatchesSearch(document, searchTerm));
        }

        filtered = ApplySorting(filtered, query.Sort, query.Direction);

        var items = filtered
            .Select(document => MapListItem(document, requestingUserId, department))
            .ToList();

        return new DocumentListResponse
        {
            Items = items,
            TotalCount = items.Count,
            AppliedScope = query.Scope
        };
    }

    public async Task<IReadOnlyList<DocumentListItem>> GetProjectDocumentsAsync(int projectId, int requestingUserId, string? department, CancellationToken cancellationToken = default)
    {
        if (!await CanAccessProjectAsync(projectId, requestingUserId, cancellationToken))
        {
            return Array.Empty<DocumentListItem>();
        }

        var result = await GetDocumentsAsync(requestingUserId, department, new DocumentQueryRequest
        {
            Scope = "accessible",
            ProjectId = projectId,
            Sort = "uploadDate",
            Direction = "desc"
        }, cancellationToken);

        return result.Items;
    }

    public async Task<IReadOnlyList<DocumentListItem>> GetTaskDocumentsAsync(int taskId, int requestingUserId, string? department, CancellationToken cancellationToken = default)
    {
        var task = await _context.Tasks
            .Include(item => item.Project)
            .ThenInclude(project => project!.ProjectMembers)
            .FirstOrDefaultAsync(item => item.TaskId == taskId, cancellationToken);

        if (task is null || !CanAccessTask(task, requestingUserId))
        {
            return Array.Empty<DocumentListItem>();
        }

        var result = await GetDocumentsAsync(requestingUserId, department, new DocumentQueryRequest
        {
            Scope = "accessible",
            TaskId = taskId,
            Sort = "uploadDate",
            Direction = "desc"
        }, cancellationToken);

        return result.Items;
    }

    public async Task<DocumentDetailsResponse?> GetDocumentByIdAsync(int documentId, int requestingUserId, string? department, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentQuery()
            .FirstOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);

        if (document is null || !CanRead(document, requestingUserId, department) || !IsVisible(document, requestingUserId, department))
        {
            return null;
        }

        return MapDetails(document, requestingUserId, department);
    }

    public async Task<DocumentDetailsResponse> UploadDocumentAsync(DocumentUploadRequest request, int requestingUserId, string? department, CancellationToken cancellationToken = default)
    {
        ValidateUploadRequest(request);

        if (!await CanUploadInContextAsync(request, requestingUserId, cancellationToken))
        {
            throw new InvalidOperationException("You are not authorized to upload in the selected context.");
        }

        var screeningResult = await _fileScreeningService.ScreenAsync(request.FileName, request.ContentType, request.FileSizeBytes, cancellationToken);
        if (!screeningResult.IsAllowed)
        {
            throw new InvalidOperationException(screeningResult.Outcome);
        }

        var projectSegment = request.ProjectId?.ToString() ?? "personal";
        var saveResult = await _fileStorageService.SaveAsync(requestingUserId.ToString(), projectSegment, request.FileName, request.FileContent, cancellationToken);

        var document = new Document
        {
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Category = request.Category.Trim(),
            OriginalFileName = request.FileName,
            StoredFileName = saveResult.StoredFileName,
            RelativeStoragePath = saveResult.RelativePath,
            MimeType = request.ContentType,
            FileExtension = saveResult.FileExtension,
            FileSizeBytes = request.FileSizeBytes,
            UploadedByUserId = requestingUserId,
            ProjectId = request.ProjectId,
            TaskId = request.TaskId,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
            LastScreenedUtc = DateTime.UtcNow,
            ScanStatus = _documentScanningOptions.EnableQueueDispatch ? DocumentScanStatus.Pending : DocumentScanStatus.Passed,
            ScreeningOutcome = _documentScanningOptions.EnableQueueDispatch ? "Pending" : screeningResult.Outcome,
            Tags = NormalizeTags(request.Tags)
                .Select(tag => new DocumentTag { TagValue = tag, CreatedUtc = DateTime.UtcNow })
                .ToList()
        };

        _context.Documents.Add(document);
        await _context.SaveChangesAsync(cancellationToken);

        if (_documentScanningOptions.EnableQueueDispatch)
        {
            await _fileScanDispatchService.DispatchAsync(new DocumentScanRequest
            {
                DocumentId = document.DocumentId,
                RelativeStoragePath = document.RelativeStoragePath,
                MimeType = document.MimeType,
                UploadedByUserId = document.UploadedByUserId,
                CorrelationId = Guid.NewGuid().ToString("N")
            }, cancellationToken);
        }

        await _documentActivityService.LogActivityAsync(document.DocumentId, requestingUserId, DocumentActivityType.Upload, new
        {
            document.Category,
            document.ProjectId,
            document.TaskId
        }, cancellationToken);

        var createdDocument = await GetDocumentByIdAsync(document.DocumentId, requestingUserId, department, cancellationToken);
        return createdDocument ?? throw new InvalidOperationException("Document upload succeeded, but the document could not be loaded.");
    }

    public async Task<DocumentDetailsResponse?> UpdateDocumentAsync(int documentId, DocumentUpdateRequest request, int requestingUserId, string? department, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentQuery()
            .FirstOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);

        if (document is null || !CanModify(document, requestingUserId) || document.DeletedUtc is not null)
        {
            return null;
        }

        ValidateCategory(request.Category);
        document.Title = request.Title.Trim();
        document.Category = request.Category.Trim();
        document.Description = request.Description?.Trim();
        document.UpdatedUtc = DateTime.UtcNow;

        document.Tags.Clear();
        foreach (var tag in NormalizeTags(request.Tags))
        {
            document.Tags.Add(new DocumentTag { TagValue = tag, CreatedUtc = DateTime.UtcNow });
        }

        await _context.SaveChangesAsync(cancellationToken);
        return MapDetails(document, requestingUserId, department);
    }

    public async Task<DocumentDetailsResponse?> ReplaceDocumentAsync(int documentId, DocumentUploadRequest request, int requestingUserId, string? department, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentQuery()
            .FirstOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);

        if (document is null || !CanModify(document, requestingUserId) || document.DeletedUtc is not null)
        {
            return null;
        }

        var screeningResult = await _fileScreeningService.ScreenAsync(request.FileName, request.ContentType, request.FileSizeBytes, cancellationToken);
        if (!screeningResult.IsAllowed)
        {
            throw new InvalidOperationException(screeningResult.Outcome);
        }

        var projectSegment = document.ProjectId?.ToString() ?? "personal";
        var saveResult = await _fileStorageService.SaveAsync(document.UploadedByUserId.ToString(), projectSegment, request.FileName, request.FileContent, cancellationToken);
        var oldPath = document.RelativeStoragePath;

        document.OriginalFileName = request.FileName;
        document.StoredFileName = saveResult.StoredFileName;
        document.RelativeStoragePath = saveResult.RelativePath;
        document.MimeType = request.ContentType;
        document.FileExtension = saveResult.FileExtension;
        document.FileSizeBytes = request.FileSizeBytes;
        document.UpdatedUtc = DateTime.UtcNow;
        document.LastScreenedUtc = DateTime.UtcNow;
        document.ScanStatus = _documentScanningOptions.EnableQueueDispatch ? DocumentScanStatus.Pending : DocumentScanStatus.Passed;
        document.ScreeningOutcome = _documentScanningOptions.EnableQueueDispatch ? "Pending" : screeningResult.Outcome;

        await _context.SaveChangesAsync(cancellationToken);
        await _fileStorageService.DeleteAsync(oldPath, cancellationToken);
        await _documentActivityService.LogActivityAsync(document.DocumentId, requestingUserId, DocumentActivityType.Replacement, new { OldPath = oldPath }, cancellationToken);

        return MapDetails(document, requestingUserId, department);
    }

    public async Task<bool> DeleteDocumentAsync(int documentId, int requestingUserId, string? department, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentQuery()
            .FirstOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);

        if (document is null || !CanDelete(document, requestingUserId) || document.DeletedUtc is not null)
        {
            return false;
        }

        document.DeletedUtc = DateTime.UtcNow;
        document.UpdatedUtc = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        await _fileStorageService.DeleteAsync(document.RelativeStoragePath, cancellationToken);
        await _documentActivityService.LogActivityAsync(document.DocumentId, requestingUserId, DocumentActivityType.Delete, null, cancellationToken);

        return true;
    }

    public async Task<DocumentShareResponse?> ShareDocumentAsync(int documentId, DocumentShareRequest request, int requestingUserId, string? department, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentQuery()
            .FirstOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);

        if (document is null || !CanShare(document, requestingUserId) || document.DeletedUtc is not null)
        {
            return null;
        }

        if ((request.UserId.HasValue && !string.IsNullOrWhiteSpace(request.Department)) || (!request.UserId.HasValue && string.IsNullOrWhiteSpace(request.Department)))
        {
            throw new InvalidOperationException("Specify either a user or a department share target.");
        }

        var normalizedDepartment = request.Department?.Trim();
        var duplicate = document.Shares.Any(share =>
            share.SharedWithUserId == request.UserId
            && string.Equals(share.SharedWithDepartment, normalizedDepartment, StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            throw new InvalidOperationException("The document is already shared with that target.");
        }

        var share = new DocumentShare
        {
            DocumentId = document.DocumentId,
            SharedByUserId = requestingUserId,
            SharedWithUserId = request.UserId,
            SharedWithDepartment = normalizedDepartment,
            AccessLevel = "Read",
            CreatedUtc = DateTime.UtcNow
        };

        _context.DocumentShares.Add(share);
        await _context.SaveChangesAsync(cancellationToken);

        await _documentActivityService.LogActivityAsync(document.DocumentId, requestingUserId, DocumentActivityType.Share, new
        {
            share.SharedWithUserId,
            share.SharedWithDepartment
        }, cancellationToken);

        await NotifyShareRecipientsAsync(document, share, cancellationToken);

        return new DocumentShareResponse
        {
            DocumentShareId = share.DocumentShareId,
            DocumentId = share.DocumentId,
            SharedByUserId = share.SharedByUserId,
            SharedWithUserId = share.SharedWithUserId,
            SharedWithDepartment = share.SharedWithDepartment,
            AccessLevel = share.AccessLevel,
            CreatedUtc = share.CreatedUtc
        };
    }

    public async Task<DocumentFileResponse?> DownloadDocumentAsync(int documentId, int requestingUserId, string? department, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentQuery()
            .FirstOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);

        if (document is null || !CanRead(document, requestingUserId, department) || !IsAvailable(document))
        {
            return null;
        }

        var stream = await _fileStorageService.OpenReadAsync(document.RelativeStoragePath, cancellationToken);
        await _documentActivityService.LogActivityAsync(document.DocumentId, requestingUserId, DocumentActivityType.Download, null, cancellationToken);

        return new DocumentFileResponse
        {
            Stream = stream,
            MimeType = document.MimeType,
            FileName = document.OriginalFileName
        };
    }

    public async Task<DocumentFileResponse?> PreviewDocumentAsync(int documentId, int requestingUserId, string? department, CancellationToken cancellationToken = default)
    {
        var document = await GetDocumentQuery()
            .FirstOrDefaultAsync(item => item.DocumentId == documentId, cancellationToken);

        if (document is null || !CanRead(document, requestingUserId, department) || !IsAvailable(document))
        {
            return null;
        }

        if (!PreviewableExtensions.Contains(document.FileExtension, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("This file type cannot be previewed in the browser.");
        }

        var stream = await _fileStorageService.OpenReadAsync(document.RelativeStoragePath, cancellationToken);
        await _documentActivityService.LogActivityAsync(document.DocumentId, requestingUserId, DocumentActivityType.Preview, null, cancellationToken);

        return new DocumentFileResponse
        {
            Stream = stream,
            MimeType = document.MimeType,
            FileName = document.OriginalFileName
        };
    }

    public async Task<IReadOnlyList<DocumentListItem>> GetRecentDocumentsAsync(int requestingUserId, string? department, int count, CancellationToken cancellationToken = default)
    {
        var documents = await GetDocumentsAsync(requestingUserId, department, new DocumentQueryRequest
        {
            Scope = "my",
            Sort = "uploadDate",
            Direction = "desc"
        }, cancellationToken);

        return documents.Items.Take(count).ToList();
    }

    public async Task<int> GetAccessibleDocumentCountAsync(int requestingUserId, string? department, CancellationToken cancellationToken = default)
    {
        var documents = await GetDocumentsAsync(requestingUserId, department, new DocumentQueryRequest
        {
            Scope = "accessible",
            Sort = "uploadDate",
            Direction = "desc"
        }, cancellationToken);

        return documents.TotalCount;
    }

    private IQueryable<Document> GetDocumentQuery()
    {
        return _context.Documents
            .Include(document => document.UploadedByUser)
            .Include(document => document.Project)
            .Include(document => document.Task)
            .Include(document => document.Tags)
            .Include(document => document.Shares);
    }

    private async Task<bool> CanUploadInContextAsync(DocumentUploadRequest request, int requestingUserId, CancellationToken cancellationToken)
    {
        if (request.ProjectId.HasValue)
        {
            if (!await CanAccessProjectAsync(request.ProjectId.Value, requestingUserId, cancellationToken))
            {
                return false;
            }
        }

        if (request.TaskId.HasValue)
        {
            var task = await _context.Tasks.FindAsync([request.TaskId.Value], cancellationToken);
            if (task is null)
            {
                return false;
            }

            if (task.ProjectId is null || request.ProjectId != task.ProjectId)
            {
                return false;
            }
        }

        return true;
    }

    private async Task<bool> CanAccessProjectAsync(int projectId, int requestingUserId, CancellationToken cancellationToken)
    {
        return await _context.Projects
            .AnyAsync(project => project.ProjectId == projectId && (
                project.ProjectManagerId == requestingUserId
                || project.ProjectMembers.Any(member => member.UserId == requestingUserId)), cancellationToken);
    }

    private static bool CanAccessTask(TaskItem task, int requestingUserId)
    {
        return task.AssignedUserId == requestingUserId
            || task.CreatedByUserId == requestingUserId
            || (task.Project?.ProjectManagerId == requestingUserId)
            || (task.Project?.ProjectMembers.Any(member => member.UserId == requestingUserId) ?? false);
    }

    private static bool MatchesSearch(Document document, string searchTerm)
    {
        return document.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || (!string.IsNullOrWhiteSpace(document.Description) && document.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            || document.Tags.Any(tag => tag.TagValue.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
            || document.UploadedByUser.DisplayName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
            || (document.Project?.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private static IEnumerable<Document> ApplySorting(IEnumerable<Document> documents, string sort, string direction)
    {
        var descending = !string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase);

        return (sort.ToLowerInvariant(), descending) switch
        {
            ("title", true) => documents.OrderByDescending(document => document.Title),
            ("title", false) => documents.OrderBy(document => document.Title),
            ("category", true) => documents.OrderByDescending(document => document.Category),
            ("category", false) => documents.OrderBy(document => document.Category),
            ("filesize", true) => documents.OrderByDescending(document => document.FileSizeBytes),
            ("filesize", false) => documents.OrderBy(document => document.FileSizeBytes),
            (_, true) => documents.OrderByDescending(document => document.CreatedUtc),
            _ => documents.OrderBy(document => document.CreatedUtc)
        };
    }

    private async Task NotifyShareRecipientsAsync(Document document, DocumentShare share, CancellationToken cancellationToken)
    {
        if (share.SharedWithUserId.HasValue)
        {
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = share.SharedWithUserId.Value,
                Title = "Document Shared With You",
                Message = $"{document.UploadedByUser.DisplayName} shared '{document.Title}' with you.",
                Type = NotificationType.DocumentShared,
                Priority = NotificationPriority.Important
            });
            return;
        }

        if (string.IsNullOrWhiteSpace(share.SharedWithDepartment))
        {
            return;
        }

        var recipients = await _context.Users
            .Where(user => user.Department == share.SharedWithDepartment && user.UserId != share.SharedByUserId)
            .ToListAsync(cancellationToken);

        foreach (var recipient in recipients)
        {
            await _notificationService.CreateNotificationAsync(new Notification
            {
                UserId = recipient.UserId,
                Title = "Department Document Shared",
                Message = $"{document.UploadedByUser.DisplayName} shared '{document.Title}' with the {share.SharedWithDepartment} department.",
                Type = NotificationType.DocumentShared,
                Priority = NotificationPriority.Important
            });
        }
    }

    private static void ValidateUploadRequest(DocumentUploadRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            throw new InvalidOperationException("Document title is required.");
        }

        ValidateCategory(request.Category);
    }

    private static void ValidateCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category) || !AllowedCategories.Contains(category.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A valid document category is required.");
        }
    }

    private static IReadOnlyList<string> NormalizeTags(IEnumerable<string> tags)
    {
        return tags
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Select(tag => tag.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private bool CanRead(Document document, int requestingUserId, string? department)
    {
        return IsAdministrator(requestingUserId)
            || document.UploadedByUserId == requestingUserId
            || HasProjectAccess(document, requestingUserId)
            || HasExplicitShare(document, requestingUserId, department);
    }

    private bool CanModify(Document document, int requestingUserId)
    {
        return IsAdministrator(requestingUserId)
            || document.UploadedByUserId == requestingUserId
            || IsProjectManager(document, requestingUserId);
    }

    private bool CanDelete(Document document, int requestingUserId)
    {
        return CanModify(document, requestingUserId);
    }

    private bool CanShare(Document document, int requestingUserId)
    {
        return IsAdministrator(requestingUserId) || document.UploadedByUserId == requestingUserId;
    }

    private bool HasProjectAccess(Document document, int requestingUserId)
    {
        return IsProjectManager(document, requestingUserId)
            || (document.Project?.ProjectMembers.Any(member => member.UserId == requestingUserId) ?? false);
    }

    private bool IsProjectManager(Document document, int requestingUserId)
    {
        return document.Project?.ProjectManagerId == requestingUserId;
    }

    private bool HasExplicitShare(Document document, int requestingUserId, string? department)
    {
        return document.Shares.Any(share =>
            share.SharedWithUserId == requestingUserId
            || (!string.IsNullOrWhiteSpace(department)
                && !string.IsNullOrWhiteSpace(share.SharedWithDepartment)
                && string.Equals(share.SharedWithDepartment, department, StringComparison.OrdinalIgnoreCase)));
    }

    private bool IsVisible(Document document, int requestingUserId, string? department)
    {
        if (document.DeletedUtc is not null)
        {
            return false;
        }

        if (IsAvailable(document))
        {
            return true;
        }

        return document.UploadedByUserId == requestingUserId || IsAdministrator(requestingUserId);
    }

    private static bool IsAvailable(Document document)
    {
        return document.DeletedUtc is null && document.ScanStatus == DocumentScanStatus.Passed;
    }

    private bool IsAdministrator(int requestingUserId)
    {
        return _context.Users.Any(user => user.UserId == requestingUserId && user.Role == UserRole.Administrator);
    }

    private DocumentListItem MapListItem(Document document, int requestingUserId, string? department)
    {
        return new DocumentListItem
        {
            DocumentId = document.DocumentId,
            Title = document.Title,
            Category = document.Category,
            OriginalFileName = document.OriginalFileName,
            MimeType = document.MimeType,
            FileSizeBytes = document.FileSizeBytes,
            UploadedByDisplayName = document.UploadedByUser.DisplayName,
            UploadedByUserId = document.UploadedByUserId,
            ProjectId = document.ProjectId,
            ProjectName = document.Project?.Name,
            TaskId = document.TaskId,
            TaskTitle = document.Task?.Title,
            CreatedUtc = document.CreatedUtc,
            UpdatedUtc = document.UpdatedUtc,
            Tags = document.Tags.Select(tag => tag.TagValue).OrderBy(tag => tag).ToList(),
            ScanStatus = document.ScanStatus,
            ScreeningOutcome = document.ScreeningOutcome,
            CanPreview = PreviewableExtensions.Contains(document.FileExtension, StringComparer.OrdinalIgnoreCase) && CanRead(document, requestingUserId, department) && IsAvailable(document),
            CanDownload = CanRead(document, requestingUserId, department) && IsAvailable(document),
            CanEdit = CanModify(document, requestingUserId),
            CanDelete = CanDelete(document, requestingUserId),
            CanShare = CanShare(document, requestingUserId)
        };
    }

    private DocumentDetailsResponse MapDetails(Document document, int requestingUserId, string? department)
    {
        var item = MapListItem(document, requestingUserId, department);
        return new DocumentDetailsResponse
        {
            DocumentId = item.DocumentId,
            Title = item.Title,
            Category = item.Category,
            OriginalFileName = item.OriginalFileName,
            MimeType = item.MimeType,
            FileSizeBytes = item.FileSizeBytes,
            UploadedByDisplayName = item.UploadedByDisplayName,
            UploadedByUserId = item.UploadedByUserId,
            ProjectId = item.ProjectId,
            ProjectName = item.ProjectName,
            TaskId = item.TaskId,
            TaskTitle = item.TaskTitle,
            CreatedUtc = item.CreatedUtc,
            UpdatedUtc = item.UpdatedUtc,
            Tags = item.Tags,
            ScanStatus = item.ScanStatus,
            ScreeningOutcome = item.ScreeningOutcome,
            CanPreview = item.CanPreview,
            CanDownload = item.CanDownload,
            CanEdit = item.CanEdit,
            CanDelete = item.CanDelete,
            CanShare = item.CanShare,
            Description = document.Description
        };
    }
}