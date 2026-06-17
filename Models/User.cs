using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusActivitySystem.Models
{
    public class User
    {
        [Key]
        public long Id { get; set; }
        [Required, MaxLength(64)]
        public string Account { get; set; }
        [Required, MaxLength(255)]
        public string PasswordHash { get; set; }
        [Required, MaxLength(64)]
        public string Name { get; set; }
        [MaxLength(32)]
        public string StudentNo { get; set; }
        [MaxLength(128)]
        public string College { get; set; }
        [MaxLength(32)]
        public string Phone { get; set; }
        [Required, MaxLength(16)]
        public string Status { get; set; } = "ACTIVE";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
        public ICollection<UserRole> UserRoles { get; set; }
    }
}