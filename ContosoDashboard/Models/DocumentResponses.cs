namespace ContosoDashboard.Models;

public class DocumentListItem
{
    public int DocumentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string OriginalFileName { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public string UploadedByDisplayName { get; set; } = string.Empty;

    public int UploadedByUserId { get; set; }

    public int? ProjectId { get; set; }

    public string? ProjectName { get; set; }

    public int? TaskId { get; set; }

    public string? TaskTitle { get; set; }

    public DateTime CreatedUtc { get; set; }

    public DateTime UpdatedUtc { get; set; }

    public IReadOnlyList<string> Tags { get; set; } = Array.Empty<string>();

    public DocumentScanStatus ScanStatus { get; set; }

    public string ScreeningOutcome { get; set; } = string.Empty;

    public bool CanPreview { get; set; }

    public bool CanDownload { get; set; }

    public bool CanEdit { get; set; }

    public bool CanDelete { get; set; }

    public bool CanShare { get; set; }
}

public class DocumentDetailsResponse : DocumentListItem
{
    public string? Description { get; set; }
}

public class DocumentShareResponse
{
    public int DocumentShareId { get; set; }

    public int DocumentId { get; set; }

    public int SharedByUserId { get; set; }

    public int? SharedWithUserId { get; set; }

    public string? SharedWithDepartment { get; set; }

    public string AccessLevel { get; set; } = "Read";

    public DateTime CreatedUtc { get; set; }
}

public class DocumentListResponse
{
    public IReadOnlyList<DocumentListItem> Items { get; set; } = Array.Empty<DocumentListItem>();

    public int TotalCount { get; set; }

    public string AppliedScope { get; set; } = "my";
}

public class DocumentFileResponse
{
    public Stream Stream { get; set; } = Stream.Null;

    public string MimeType { get; set; } = "application/octet-stream";

    public string FileName { get; set; } = string.Empty;
}

public class DocumentActivitySummaryResponse
{
    public IReadOnlyList<DocumentActivitySummaryItem> ActivityByType { get; set; } = Array.Empty<DocumentActivitySummaryItem>();

    public IReadOnlyList<DocumentUploaderSummaryItem> ActiveUploaders { get; set; } = Array.Empty<DocumentUploaderSummaryItem>();

    public IReadOnlyList<DocumentActivityEventItem> RecentEvents { get; set; } = Array.Empty<DocumentActivityEventItem>();
}

public class DocumentActivitySummaryItem
{
    public string ActivityType { get; set; } = string.Empty;

    public int Count { get; set; }
}

public class DocumentUploaderSummaryItem
{
    public int UserId { get; set; }

    public string DisplayName { get; set; } = string.Empty;

    public int UploadCount { get; set; }
}

public class DocumentActivityEventItem
{
    public int DocumentId { get; set; }

    public string DocumentTitle { get; set; } = string.Empty;

    public string ActorDisplayName { get; set; } = string.Empty;

    public string ActivityType { get; set; } = string.Empty;

    public DateTime OccurredUtc { get; set; }
}