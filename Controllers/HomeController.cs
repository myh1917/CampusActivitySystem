using CampusActivitySystem.Data;
using CampusActivitySystem.Models;
using CampusActivitySystem.Models.ViewModels;
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
    // ��ҳ��չʾ����Ļ
    public async Task<IActionResult> Index()
    {
        // ���ά��ģʽ
        var maintenance = await _context.SystemConfigs
            .FirstOrDefaultAsync(c => c.ConfigKey == "MaintenanceMode");
        if (maintenance != null && maintenance.ConfigValue == "true")
        {
            // ֻ�й���Ա���Է���
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

    // ======= ����Ԥ�� Action ���ֱ�������������ҳ�� 404���������ض��� =======
    public IActionResult Login() => RedirectToAction("Login", "Account");
    public IActionResult Register() => RedirectToAction("Register", "Account");
    public IActionResult ActivityList() => RedirectToAction("Index", "Activity");
    public IActionResult SignIn() => View();                 // ǩ��ҳ��ʱ������̬
    public IActionResult Profile() => RedirectToAction("Info", "Account");
    public IActionResult MyRegistrations() => RedirectToAction("MyRegistrations", "Registration");
    public IActionResult AdminIndex() => View();             // ��̨��ҳ��̬
    public IActionResult AdminActivityManage() => RedirectToAction("Create", "Activity");
    public IActionResult SignControl() => View();            // ǩ������̨��̬
    public IActionResult Statistics() => View();             // ͳ�Ƶ�����̬
    public IActionResult Notices() => RedirectToAction("Index", "Notice");
    /// <summary>
    /// 我的活动 — 展示当前用户作为组织者创建的所有活动及其统计数据
    /// </summary>
    public async Task<IActionResult> MyActivities()
    {
        // 1. 检查登录
        var userIdStr = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            return RedirectToAction("Login", "Account");

        // 2. 查询当前用户作为组织者的所有活动
        var activities = await _context.Activities
            .Where(a => a.OrganizerId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        // 3. 获取每个活动的签到人次：通过 SignInSession -> SignIn 关联
        var activityIds = activities.Select(a => a.Id).ToList();
        var signInCounts = new Dictionary<long, int>();
        var pendingAuditCounts = new Dictionary<long, int>();

        if (activityIds.Count > 0)
        {
            // 签到人次（按活动聚合）
            var signInQuery = await _context.SignIns
                .Include(s => s.Session)
                .Where(s => activityIds.Contains(s.Session.ActivityId))
                .GroupBy(s => s.Session.ActivityId)
                .Select(g => new { ActivityId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var item in signInQuery)
                signInCounts[item.ActivityId] = item.Count;

            // 待审核报名数
            var pendingQuery = await _context.Registrations
                .Where(r => activityIds.Contains(r.ActivityId) && r.Status == "PENDING")
                .GroupBy(r => r.ActivityId)
                .Select(g => new { ActivityId = g.Key, Count = g.Count() })
                .ToListAsync();

            foreach (var item in pendingQuery)
                pendingAuditCounts[item.ActivityId] = item.Count;
        }

        // 4. 构建 ViewModel
        var items = activities.Select(a => new MyActivityItem
        {
            Id = a.Id,
            Title = a.Title,
            Category = a.Category ?? "",
            Status = a.Status,
            Location = a.Location,
            Capacity = a.Capacity,
            RegisteredCount = a.RegisteredCount,
            SignedCount = signInCounts.TryGetValue(a.Id, out var sc) ? sc : 0,
            PendingAuditCount = pendingAuditCounts.TryGetValue(a.Id, out var pa) ? pa : 0,
            StartTime = a.StartTime,
            CreatedAt = a.CreatedAt,
            NeedAudit = a.NeedAudit
        }).ToList();

        var vm = new MyActivitiesViewModel
        {
            Activities = items,
            TotalActivities = items.Count,
            TotalRegistrations = items.Sum(i => i.RegisteredCount),
            TotalSignIns = items.Sum(i => i.SignedCount),
            NeedsAttention = items.Count(i => i.PendingAuditCount > 0)
        };

        return View(vm);
    }
}