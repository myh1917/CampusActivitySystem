using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusActivitySystem.Models
{
    public class Activity
    {
        [Key]
        public long Id { get; set; }
        [Required, MaxLength(200)]
        public string Title { get; set; }
        [MaxLength(64)]
        public string Category { get; set; }
        public long OrganizerId { get; set; }
        [ForeignKey("OrganizerId")] public User Organizer { get; set; }
        public string Description { get; set; } = "";
        [Required, MaxLength(255)]
        public string Location { get; set; }
        public int Capacity { get; set; }
        public int RegisteredCount { get; set; } = 0;
        public DateTime RegisterStart { get; set; }
        public DateTime RegisterEnd { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool NeedAudit { get; set; }
        public bool AllowWaitlist { get; set; }
        [MaxLength(24)]
        public string Status { get; set; } = "DRAFT";
        public int Version { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public ICollection<Registration> Registrations { get; set; }
        public ICollection<SignInSession> SignInSessions { get; set; }
    }
}