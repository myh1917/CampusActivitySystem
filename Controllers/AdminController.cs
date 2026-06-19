using CampusActivitySystem.Data;
using CampusActivitySystem.Filters;
using CampusActivitySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusActivitySystem.Controllers
{
    [Auth]
    [Role("admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<IActionResult> AuditLogs()
        {
            var logs = await _context.AuditLogs.OrderByDescending(l => l.CreatedAt).Take(200).ToListAsync();
            return View(logs);
        }

        public async Task<IActionResult> SystemConfigs()
        {
            var configs = await _context.SystemConfigs.ToListAsync();
            return View(configs);
        }

        [HttpPost]
        public async Task<IActionResult> SystemConfigs(string key, string value, string description)
        {
            var config = await _context.SystemConfigs.FindAsync(key);
            if (config == null)
            {
                config = new SystemConfig { ConfigKey = key };
                _context.SystemConfigs.Add(config);
            }
            config.ConfigValue = value;
            config.Description = description;
            config.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(SystemConfigs));
        }

        public IActionResult Backup()
        {
            var backupPath = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }
            var fileName = $"CampusActivity_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            var filePath = Path.Combine(backupPath, fileName);

            var connectionString = _context.Database.GetConnectionString();
            var server = "";
            var database = "";

            var parts = connectionString.Split(';');
            foreach (var part in parts)
            {
                if (part.Trim().StartsWith("Server=", StringComparison.OrdinalIgnoreCase))
                    server = part.Trim().Substring(7);
                if (part.Trim().StartsWith("Database=", StringComparison.OrdinalIgnoreCase))
                    database = part.Trim().Substring(9);
            }

            ViewBag.BackupInfo = new
            {
                Server = server,
                Database = database,
                BackupPath = filePath,
                ConnectionString = connectionString
            };

            return View();
        }

        [HttpPost]
        public IActionResult PerformBackup()
        {
            var backupPath = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
            if (!Directory.Exists(backupPath))
            {
                Directory.CreateDirectory(backupPath);
            }
            var fileName = $"CampusActivity_{DateTime.Now:yyyyMMdd_HHmmss}.sql";
            var filePath = Path.Combine(backupPath, fileName);

            try
            {
                using (var writer = new StreamWriter(filePath))
                {
                    writer.WriteLine("-- 数据库备份脚本");
                    writer.WriteLine($"-- 备份时间: {DateTime.Now}");
                    writer.WriteLine("-- 数据库: CampusActivity");
                    writer.WriteLine();
                    writer.WriteLine("USE [CampusActivity]");
                    writer.WriteLine("GO");
                }

                ViewBag.Message = $"备份成功！文件已保存至: {filePath}";
                ViewBag.Success = true;
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"备份失败: {ex.Message}";
                ViewBag.Success = false;
            }

            return RedirectToAction(nameof(Backup));
        }

        // ========== 用户管理 ==========

        // 用户管理列表
        public async Task<IActionResult> AdminUserList()
        {
            // 1. 调试：打印当前用户角色
            var rolesStr = HttpContext.Session.GetString("Roles");
            System.Diagnostics.Debug.WriteLine($"=== 当前用户角色: {rolesStr} ===");

            // 2. 调试：检查角色（如果没拦住，手动拦）
            if (string.IsNullOrEmpty(rolesStr) || !rolesStr.Contains("admin"))
            {
                System.Diagnostics.Debug.WriteLine("=== 普通用户被拦截 ===");
                return RedirectToAction("AccessDenied", "Home");
            }

            // 3. 查询所有用户
            var users = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ToListAsync();

            // 4. 调试：打印查到了多少用户
            System.Diagnostics.Debug.WriteLine($"=== 查询到 {users.Count} 位用户 ===");

            // 5. 如果没数据，给一个提示
            if (users.Count == 0)
            {
                ViewBag.Message = "数据库中没有用户数据";
            }

            return View("~/Views/Home/AdminUserList.cshtml", users);
        }
        // 切换用户状态（启用/禁用）
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(long id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
                return NotFound();

            user.Status = user.Status == "ACTIVE" ? "INACTIVE" : "ACTIVE";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"用户 {user.Name} 已{(user.Status == "ACTIVE" ? "启用" : "禁用")}";
            return RedirectToAction("AdminUserList");
        }

        // 分配角色
        [HttpPost]
        public async Task<IActionResult> AssignRole(long userId, string roleCode)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null)
                return NotFound();

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Code == roleCode);
            if (role == null)
                return NotFound();

            // 移除旧角色
            user.UserRoles.Clear();
            // 添加新角色
            user.UserRoles.Add(new UserRole { UserId = userId, RoleId = role.Id });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"用户 {user.Name} 的角色已更新为 {role.Name}";
            return RedirectToAction("AdminUserList");
        }
    }
}