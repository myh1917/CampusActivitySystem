using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusActivitySystem.Models
{
    public class SignIn
    {
        [Key]
        public long Id { get; set; }
        public long SessionId { get; set; }
        [ForeignKey("SessionId")] public SignInSession Session { get; set; }
        public long RegistrationId { get; set; }
        [ForeignKey("RegistrationId")] public Registration Registration { get; set; }
        [MaxLength(16)]
        public string Method { get; set; }
        public DateTime CheckedAt { get; set; } = DateTime.Now;
        public long? OperatorId { get; set; }
        public string ManualReason { get; set; }
        [MaxLength(64)]
        public string IpAddress { get; set; }
    }
}