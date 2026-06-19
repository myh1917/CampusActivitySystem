using CampusActivitySystem.Data;
using CampusActivitySystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusActivitySystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly AppDbContext _context;

    public HomeController(ILogger<HomeController> logger, AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    [AllowAnonymous]
    public IActionResult Maintenance()
    {
        return View();
    }
    // 首页：展示最近的活动
    public async Task<IActionResult> Index()
    {
        // 检查维护模式
        var maintenance = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.ConfigKey == "MaintenanceMode");
        if (maintenance != null && maintenance.ConfigValue == "true")
        {
            // 只有管理员可以访问
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId) || !User.IsInRole("admin"))
            {
                return View("Maintenance");
            }
        }

        var activities = await _context.Activities
            .Where(a => a.Status == "PUBLISHED")
            .OrderByDescending(a => a.CreatedAt)
            .Take(6)
            .ToListAsync();
        return View(activities);
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    public IActionResult AccessDenied() => View();

    // ======= 以下预览 Action 部分保留，避免其他页面 404，但可以重定向 =======
    public IActionResult Login() => RedirectToAction("Login", "Account");
    public IActionResult Register() => RedirectToAction("Register", "Account");
    public IActionResult ActivityList() => RedirectToAction("Index", "Activity");
    public IActionResult SignIn() => View();                 // 签到页暂时保留静态
    public IActionResult Profile() => RedirectToAction("Info", "Account");
    public IActionResult MyRegistrations() => RedirectToAction("MyRegistrations", "Registration");
    public IActionResult AdminIndex() => View();             // 后台首页静态
    public IActionResult AdminActivityManage() => RedirectToAction("Create", "Activity");
    public IActionResult SignControl() => View();            // 签到控制台静态
    public IActionResult Statistics() => View();             // 统计导出静态
    public IActionResult Notices() => RedirectToAction("Index", "Notice");
    public IActionResult MyActivities() => View();           // 我的活动静态（可后续完善）
}