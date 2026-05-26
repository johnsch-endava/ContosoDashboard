using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class Document
{
    [Key]
    public int DocumentId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(100)]
    public string Category { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string StoredFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string RelativeStoragePath { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string MimeType { get; set; } = string.Empty;

    [Required]
    [MaxLength(20)]
    public string FileExtension { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    [Required]
    public int UploadedByUserId { get; set; }

    public int? ProjectId { get; set; }

    public int? TaskId { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DeletedUtc { get; set; }

    public DateTime? LastScreenedUtc { get; set; }

    [Required]
    public DocumentScanStatus ScanStatus { get; set; } = DocumentScanStatus.Passed;

    [Required]
    [MaxLength(255)]
    public string ScreeningOutcome { get; set; } = "Passed";

    [ForeignKey(nameof(UploadedByUserId))]
    public virtual User UploadedByUser { get; set; } = null!;

    [ForeignKey(nameof(ProjectId))]
    public virtual Project? Project { get; set; }

    [ForeignKey(nameof(TaskId))]
    public virtual TaskItem? Task { get; set; }

    public virtual ICollection<DocumentTag> Tags { get; set; } = new List<DocumentTag>();

    public virtual ICollection<DocumentShare> Shares { get; set; } = new List<DocumentShare>();

    public virtual ICollection<DocumentActivityRecord> ActivityRecords { get; set; } = new List<DocumentActivityRecord>();
}

public enum DocumentScanStatus
{
    Pending,
    Passed,
    Failed,
    Quarantined
}