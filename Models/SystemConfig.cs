using System.ComponentModel.DataAnnotations;

namespace CampusActivitySystem.Models
{
    public class SystemConfig
    {
        [Key, MaxLength(64)]
        public string ConfigKey { get; set; } = string.Empty;
        public string? ConfigValue { get; set; }
        public string? Description { get; set; }
        public long? UpdatedBy { get; set; }
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}