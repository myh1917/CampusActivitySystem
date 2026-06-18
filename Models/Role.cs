using System.ComponentModel.DataAnnotations;

namespace CampusActivitySystem.Models
{
    public class Role
    {
        [Key]
        public long Id { get; set; }
        [Required, MaxLength(64)]
        public string Code { get; set; } = string.Empty;
        [Required, MaxLength(64)]
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public ICollection<RolePermission>? RolePermissions { get; set; }
        public ICollection<UserRole>? UserRoles { get; set; }
    }
}