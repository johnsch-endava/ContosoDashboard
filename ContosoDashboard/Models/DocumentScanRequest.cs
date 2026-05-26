namespace ContosoDashboard.Models;

public class DocumentScanRequest
{
    public int DocumentId { get; set; }

    public string RelativeStoragePath { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public int UploadedByUserId { get; set; }

    public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
}