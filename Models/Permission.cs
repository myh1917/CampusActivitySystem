using System.ComponentModel.DataAnnotations;

namespace CampusActivitySystem.Models
{
    public class Permission
    {
        [Key]
        public long Id { get; set; }
        [Required, MaxLength(64)]
        public string Code { get; set; }
        [Required, MaxLength(128)]
        public string Name { get; set; }
        public string Module { get; set; }
        public ICollection<RolePermission> RolePermissions { get; set; }
    }
}