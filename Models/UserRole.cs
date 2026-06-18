using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusActivitySystem.Models
{
    public class UserRole
    {
        [Key]
        public long Id { get; set; }
        public long UserId { get; set; }
        [ForeignKey("UserId")] public User User { get; set; }
        public long RoleId { get; set; }
        [ForeignKey("RoleId")] public Role Role { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}