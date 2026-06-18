using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusActivitySystem.Models
{
    public class Notice
    {
        [Key]
        public long Id { get; set; }
        public long UserId { get; set; }
        [ForeignKey("UserId")] public User? User { get; set; }
        [Required, MaxLength(32)]
        public string Type { get; set; } = string.Empty;
        [Required, MaxLength(128)]
        public string Title { get; set; } = string.Empty;
        [MaxLength(1000)]
        public string? Content { get; set; }
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}