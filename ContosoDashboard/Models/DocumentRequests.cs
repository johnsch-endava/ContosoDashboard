namespace ContosoDashboard.Models;

public class DocumentQueryRequest
{
    public string Scope { get; set; } = "my";

    public int? ProjectId { get; set; }

    public int? TaskId { get; set; }

    public string? Category { get; set; }

    public string? Search { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public string Sort { get; set; } = "uploadDate";

    public string Direction { get; set; } = "desc";
}

public class DocumentUploadRequest
{
    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? ProjectId { get; set; }

    public int? TaskId { get; set; }

    public List<string> Tags { get; set; } = new();

    public string FileName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public Stream FileContent { get; set; } = Stream.Null;
}

public class DocumentUpdateRequest
{
    public string Title { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<string> Tags { get; set; } = new();
}

public class DocumentShareRequest
{
    public int? UserId { get; set; }

    public string? Department { get; set; }
}