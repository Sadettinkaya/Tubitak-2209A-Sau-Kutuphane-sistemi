using System.ComponentModel.DataAnnotations;

namespace FeedbackService.Models;

public class Feedback
{
    public int Id { get; set; }

    [Required]
    [StringLength(20)]
    public string StudentNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(500)]
    public string Message { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}
