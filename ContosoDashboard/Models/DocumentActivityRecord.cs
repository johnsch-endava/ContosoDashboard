using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentActivityRecord
{
    [Key]
    public int DocumentActivityRecordId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int ActorUserId { get; set; }

    [Required]
    public DocumentActivityType ActivityType { get; set; }

    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;

    [MaxLength(4000)]
    public string? DetailsJson { get; set; }

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(ActorUserId))]
    public virtual User ActorUser { get; set; } = null!;
}

public enum DocumentActivityType
{
    Upload,
    Preview,
    Download,
    Replacement,
    Delete,
    Share
}