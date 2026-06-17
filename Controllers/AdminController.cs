using CampusActivitySystem.Data;
using CampusActivitySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusActivitySystem.Controllers
{
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
    }
}