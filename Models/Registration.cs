using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusActivitySystem.Models
{
    public class Registration
    {
        [Key]
        public long Id { get; set; }
        public long ActivityId { get; set; }
        [ForeignKey("ActivityId")] public Activity? Activity { get; set; }
        public long UserId { get; set; }
        [ForeignKey("UserId")] public User? User { get; set; }
        [MaxLength(24)]
        public string Status { get; set; } = "PENDING";
        public int? WaitNo { get; set; }
        public string? FormData { get; set; }
        public string? AuditComment { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.Now;
        public DateTime? CancelledAt { get; set; }
        public DateTime? CheckinAt { get; set; }
        public ICollection<SignIn>? SignIns { get; set; }
    }
}