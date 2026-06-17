using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CampusActivitySystem.Models
{
    public class RolePermission
    {
        [Key]
        public long Id { get; set; }
        public long RoleId { get; set; }
        [ForeignKey("RoleId")] public Role Role { get; set; }
        public long PermissionId { get; set; }
        [ForeignKey("PermissionId")] public Permission Permission { get; set; }
    }
}