using CampusActivitySystem.Models;

namespace CampusActivitySystem.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();
            if (context.Users.Any()) return;

            // 角色（补充 Description）
            var adminRole = new Role { Code = "admin", Name = "系统管理员", Description = "拥有所有权限" };
            var orgRole = new Role { Code = "organizer", Name = "活动负责人", Description = "可发布和管理活动" };
            var studentRole = new Role { Code = "student", Name = "学生", Description = "普通学生用户" };
            context.Roles.AddRange(adminRole, orgRole, studentRole);

            var perm1 = new Permission { Code = "activity:create", Name = "发布活动", Module = "活动管理" };
            var perm2 = new Permission { Code = "sign:manual", Name = "手动补签", Module = "签到管理" };
            context.Permissions.AddRange(perm1, perm2);

            var adminUser = new User
            {
                Account = "admin",
                PasswordHash = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=",
                Name = "管理员",
                StudentNo = "",
                College = "",
                Phone = "",
                Status = "ACTIVE"
            };
            var stuUser = new User
            {
                Account = "student1",
                PasswordHash = "jZae727K08KaOmKSgOaGzww/XVqGr/PKEgIMkjrcbJI=",
                Name = "小明",
                StudentNo = "2024001",
                College = "",
                Phone = "",
                Status = "ACTIVE"
            };
            context.Users.AddRange(adminUser, stuUser);

            context.UserRoles.Add(new UserRole { User = adminUser, Role = adminRole });
            context.UserRoles.Add(new UserRole { User = stuUser, Role = studentRole });

            context.RolePermissions.Add(new RolePermission { Role = adminRole, Permission = perm1 });
            context.RolePermissions.Add(new RolePermission { Role = adminRole, Permission = perm2 });

            context.SaveChanges();
        }
    }
}