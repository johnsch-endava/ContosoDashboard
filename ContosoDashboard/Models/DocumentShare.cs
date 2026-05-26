using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContosoDashboard.Models;

public class DocumentShare
{
    [Key]
    public int DocumentShareId { get; set; }

    [Required]
    public int DocumentId { get; set; }

    [Required]
    public int SharedByUserId { get; set; }

    public int? SharedWithUserId { get; set; }

    [MaxLength(100)]
    public string? SharedWithDepartment { get; set; }

    [Required]
    [MaxLength(30)]
    public string AccessLevel { get; set; } = "Read";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(DocumentId))]
    public virtual Document Document { get; set; } = null!;

    [ForeignKey(nameof(SharedByUserId))]
    public virtual User SharedByUser { get; set; } = null!;

    [ForeignKey(nameof(SharedWithUserId))]
    public virtual User? SharedWithUser { get; set; }
}