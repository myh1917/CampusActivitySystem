using CampusActivitySystem.Data;
using CampusActivitySystem.Filters;
using CampusActivitySystem.Models;
using CampusActivitySystem.Models.ViewModels;
using CampusActivitySystem.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace CampusActivitySystem.Controllers;

[Auth]
public class SignInController : Controller
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly SignInService _service;
    private readonly IDataProtector _protector;

    public SignInController(
        AppDbContext context,
        IConfiguration configuration,
        IDataProtectionProvider protectionProvider)
    {
        _context = context;
        _configuration = configuration;
        _service = new SignInService(context, configuration);
        _protector = protectionProvider.CreateProtector("CampusActivitySystem.SignIn.Qr.v1");
    }

    [HttpGet("/SignIn")]
    [HttpGet("/SignIn/Index")]
    [HttpGet("/Home/SignIn")]
    public IActionResult Index() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ByCode(string code)
    {
        var result = await _service.CheckInByCodeAsync(GetUserId(), code ?? "", GetIpAddress());
        return Json(new { result.Success, result.Message });
    }

    public IActionResult Scan(string token)
    {
        if (!TryReadQrToken(token, out _, out var message))
        {
            ViewBag.Error = message;
            token = "";
        }
        return View(model: token);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ByQr(string token)
    {
        if (!TryReadQrToken(token, out var sessionId, out var message))
            return Json(new { Success = false, Message = message });

        var result = await _service.CheckInBySessionAsync(GetUserId(), sessionId, GetIpAddress());
        return Json(new { result.Success, result.Message });
    }

    [HttpGet("/SignIn/Control")]
    [HttpGet("/Home/SignControl")]
    [Role("admin,organizer")]
    public async Task<IActionResult> Control(long? activityId)
    {
        var activities = await ManageableActivities().OrderByDescending(a => a.CreatedAt).ToListAsync();
        var selected = activityId.HasValue
            ? activities.FirstOrDefault(a => a.Id == activityId.Value)
            : activities.FirstOrDefault();

        var model = new SignControlViewModel { Activities = activities, SelectedActivity = selected };
        if (selected == null) return View(model);

        var now = DateTime.Now;
        model.ActiveSession = await _context.SignInSessions
            .Where(s => s.ActivityId == selected.Id && s.Status == "ACTIVE" && s.EndTime >= now)
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync();
        if (model.ActiveSession != null)
            model.SignCode = _service.GetDisplayCode(model.ActiveSession);

        model.RegisteredCount = await _context.Registrations.CountAsync(r =>
            r.ActivityId == selected.Id && r.Status == "REGISTERED");
        model.SignedCount = await _context.SignIns
            .Where(s => s.Session.ActivityId == selected.Id)
            .Select(s => s.RegistrationId)
            .Distinct()
            .CountAsync();
        model.RecentSignIns = await _context.SignIns
            .Include(s => s.Registration).ThenInclude(r => r.User)
            .Include(s => s.Session)
            .Where(s => s.Session.ActivityId == selected.Id)
            .OrderByDescending(s => s.CheckedAt)
            .Take(20)
            .ToListAsync();
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Role("admin,organizer")]
    public async Task<IActionResult> Start(long activityId, int durationMinutes = 30)
    {
        if (!await CanManageAsync(activityId)) return RedirectToAction("AccessDenied", "Home");
        var started = await _service.StartAsync(activityId, GetUserId(), durationMinutes);
        TempData["Success"] = $"签到已开启，将于 {started.Session.EndTime:HH:mm} 结束";
        return RedirectToAction(nameof(Control), new { activityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Role("admin,organizer")]
    public async Task<IActionResult> Close(long sessionId)
    {
        var session = await _context.SignInSessions.FindAsync(sessionId);
        if (session == null) return NotFound();
        if (!await CanManageAsync(session.ActivityId)) return RedirectToAction("AccessDenied", "Home");
        session.Status = "CLOSED";
        if (session.EndTime > DateTime.Now) session.EndTime = DateTime.Now;
        await _context.SaveChangesAsync();
        TempData["Success"] = "签到已关闭";
        return RedirectToAction(nameof(Control), new { activityId = session.ActivityId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Role("admin,organizer")]
    public async Task<IActionResult> Manual(long sessionId, string account, string reason)
    {
        var session = await _context.SignInSessions.FindAsync(sessionId);
        if (session == null) return NotFound();
        if (!await CanManageAsync(session.ActivityId)) return RedirectToAction("AccessDenied", "Home");

        account = (account ?? "").Trim();
        var registration = await _context.Registrations
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.ActivityId == session.ActivityId
                                      && r.Status == "REGISTERED"
                                      && (r.User.Account == account || r.User.StudentNo == account));
        if (registration == null)
        {
            TempData["Error"] = "未找到该账号或学号对应的有效报名";
            return RedirectToAction(nameof(Control), new { activityId = session.ActivityId });
        }

        var result = await _service.ManualAsync(sessionId, registration.Id, GetUserId(), reason ?? "", GetIpAddress());
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Control), new { activityId = session.ActivityId });
    }

    [Role("admin,organizer")]
    public async Task<IActionResult> Live(long sessionId)
    {
        var session = await _context.SignInSessions.FindAsync(sessionId);
        if (session == null || !await CanManageAsync(session.ActivityId)) return Forbid();

        var registered = await _context.Registrations.CountAsync(r =>
            r.ActivityId == session.ActivityId && r.Status == "REGISTERED");
        var signed = await _context.SignIns.CountAsync(s => s.SessionId == sessionId);
        var rawRows = await _context.SignIns
            .Include(s => s.Registration).ThenInclude(r => r.User)
            .Where(s => s.SessionId == sessionId)
            .OrderByDescending(s => s.CheckedAt)
            .Take(30)
            .Select(s => new
            {
                s.Registration.User.StudentNo,
                s.Registration.User.Name,
                s.CheckedAt,
                s.Method
            })
            .ToListAsync();
        var rows = rawRows.Select(s => new
        {
            s.StudentNo,
            s.Name,
            CheckedAt = s.CheckedAt.ToString("HH:mm:ss"),
            s.Method
        });
        return Json(new { signed, registered, rows });
    }

    [Role("admin,organizer")]
    public async Task<IActionResult> QrCode(long sessionId)
    {
        var session = await _context.SignInSessions.FindAsync(sessionId);
        if (session == null || !await CanManageAsync(session.ActivityId)) return Forbid();

        var token = _protector.Protect($"{session.Id}|{session.EndTime.ToBinary()}");
        var relativeUrl = Url.Action(nameof(Scan), "SignIn", new { token })!;
        var publicBaseUrl = _configuration["SignIn:PublicBaseUrl"]?.TrimEnd('/');
        var scanUrl = string.IsNullOrWhiteSpace(publicBaseUrl)
            ? $"{Request.Scheme}://{Request.Host}{Request.PathBase}{relativeUrl}"
            : $"{publicBaseUrl}{relativeUrl}";

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(scanUrl, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(data);
        return File(qrCode.GetGraphic(10), "image/png");
    }

    private IQueryable<Activity> ManageableActivities()
    {
        var query = _context.Activities.AsQueryable();
        return IsAdmin() ? query : query.Where(a => a.OrganizerId == GetUserId());
    }

    private async Task<bool> CanManageAsync(long activityId)
    {
        return IsAdmin() || await _context.Activities.AnyAsync(a =>
            a.Id == activityId && a.OrganizerId == GetUserId());
    }

    private bool IsAdmin() => GetRoles().Contains("admin", StringComparer.OrdinalIgnoreCase);

    private string[] GetRoles() => (HttpContext.Session.GetString("Roles") ?? "")
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private long GetUserId() => long.TryParse(HttpContext.Session.GetString("UserId"), out var id) ? id : 0;

    private string? GetIpAddress() => HttpContext.Connection.RemoteIpAddress?.ToString();

    private bool TryReadQrToken(string token, out long sessionId, out string message)
    {
        sessionId = 0;
        message = "二维码无效或已损坏";
        if (string.IsNullOrWhiteSpace(token)) return false;
        try
        {
            var parts = _protector.Unprotect(token).Split('|');
            if (parts.Length != 2 || !long.TryParse(parts[0], out sessionId)
                                  || !long.TryParse(parts[1], out var binary)) return false;
            if (DateTime.FromBinary(binary) < DateTime.Now)
            {
                message = "二维码已过期";
                return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
