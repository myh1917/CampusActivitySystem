using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusActivitySystem.Models
{
    public class SignInSession
    {
        [Key]
        public long Id { get; set; }
        public long ActivityId { get; set; }
        [ForeignKey("ActivityId")] public Activity Activity { get; set; }
        [MaxLength(16)]
        public string Mode { get; set; }
        public string SignCodeHash { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        [MaxLength(16)]
        public string Status { get; set; } = "READY";
        public long CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ICollection<SignIn> SignIns { get; set; }
    }
}