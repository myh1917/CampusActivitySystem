using System.ComponentModel.DataAnnotations;

namespace CampusActivitySystem.Models
{
    public class Role
    {
        [Key]
        public long Id { get; set; }
        [Required, MaxLength(64)]
        public string Code { get; set; }
        [Required, MaxLength(64)]
        public string Name { get; set; }
        public string Description { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; }
        public ICollection<UserRole> UserRoles { get; set; }
    }
}