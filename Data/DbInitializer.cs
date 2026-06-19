using CampusActivitySystem.Models;

namespace CampusActivitySystem.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            // ===== 临时取消注释可重置数据库 =====
            //context.Database.EnsureDeleted();
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

            // 管理员 admin / 12345678
            var adminUser = new User
            {
                Account = "admin",
                PasswordHash = "73l8gRjwLftklgfdXT+MdiMEjJwGPVMsyVxe16iYpk8=", // 12345678
                Name = "管理员",
                StudentNo = "admin",
                College = "信息中心",
                Phone = "13800000000",
                Status = "ACTIVE",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            // 测试学生
            var stuUser = new User
            {
                Account = "student1",
                PasswordHash = "73l8gRjwLftklgfdXT+MdiMEjJwGPVMsyVxe16iYpk8=",
                Name = "小明",
                StudentNo = "2024001",
                College = "计算机学院",
                Phone = "13811111111",
                Status = "ACTIVE",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            // 测试活动负责人
            var orgUser = new User
            {
                Account = "organizer1",
                PasswordHash = "73l8gRjwLftklgfdXT+MdiMEjJwGPVMsyVxe16iYpk8=",
                Name = "李老师",
                StudentNo = "T001",
                College = "信息中心",
                Phone = "13822222222",
                Status = "ACTIVE",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            context.Users.AddRange(adminUser, stuUser, orgUser);

            context.UserRoles.Add(new UserRole { User = adminUser, Role = adminRole });
            context.UserRoles.Add(new UserRole { User = stuUser, Role = studentRole });
            context.UserRoles.Add(new UserRole { User = orgUser, Role = orgRole });

            context.RolePermissions.Add(new RolePermission { Role = adminRole, Permission = perm1 });
            context.RolePermissions.Add(new RolePermission { Role = adminRole, Permission = perm2 });

            // 预置系统配置
            var configs = new List<SystemConfig>
{
    new SystemConfig { ConfigKey = "SiteName", ConfigValue = "校园活动报名与签到系统", Description = "网站名称" },
    new SystemConfig { ConfigKey = "AllowRegister", ConfigValue = "true", Description = "是否允许新用户注册（true/false）" },
    new SystemConfig { ConfigKey = "MaintenanceMode", ConfigValue = "false", Description = "系统维护模式（true/false），开启后仅管理员可访问" },
    new SystemConfig { ConfigKey = "MaxActivityPerUser", ConfigValue = "10", Description = "每个用户最多同时报名活动数" }
};
            context.SystemConfigs.AddRange(configs);

            context.SaveChanges();
        }
    }
}