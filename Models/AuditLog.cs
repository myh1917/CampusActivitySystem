using System.ComponentModel.DataAnnotations;

namespace CampusActivitySystem.Models
{
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }
        public long? OperatorId { get; set; }
        [MaxLength(64)]
        public string? Action { get; set; }
        [MaxLength(64)]
        public string? TargetType { get; set; }
        public long? TargetId { get; set; }
        public string? BeforeJson { get; set; }
        public string? AfterJson { get; set; }
        public bool Result { get; set; } = true;
        [MaxLength(64)]
        public string? IpAddress { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}