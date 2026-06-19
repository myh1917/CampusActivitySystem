using CampusActivitySystem.Data;
using CampusActivitySystem.Filters;
using CampusActivitySystem.Models;
using CampusActivitySystem.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusActivitySystem.Controllers
{
    public class ActivityController : Controller
    {
        private readonly AppDbContext _context;
        private readonly NoticeService _noticeService;

        public ActivityController(AppDbContext context, NoticeService noticeService)
        {
            _context = context;
            _noticeService = noticeService;
        }

        // 活动列表（公开）
        public async Task<IActionResult> Index(string keyword, string category)
        {
            var query = _context.Activities.Where(a => a.Status == "PUBLISHED");

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(a => a.Title.Contains(keyword));
            if (!string.IsNullOrEmpty(category))
                query = query.Where(a => a.Category == category);

            var list = await query.OrderByDescending(a => a.StartTime).ToListAsync();
            return View(list);
        }

        // 活动详情
        public async Task<IActionResult> Detail(long id)
        {
            var activity = await _context.Activities
                .Include(a => a.Organizer)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (activity == null) return NotFound();

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (!string.IsNullOrEmpty(userIdStr) && long.TryParse(userIdStr, out long userId))
            {
                ViewBag.IsRegistered = await _context.Registrations
                    .AnyAsync(r => r.ActivityId == id && r.UserId == userId
                                   && (r.Status == "REGISTERED" || r.Status == "PENDING"));
            }
            else
            {
                ViewBag.IsRegistered = false;
            }

            return View("~/Views/Home/ActivityDetail.cshtml", activity);
        }

        // =============== 发布活动 ===============
        [HttpGet]
        [Auth]
        [Role("admin,organizer")]
        public IActionResult Create()
        {
            ViewBag.IsEdit = false;
            var now = DateTime.Now;
            var model = new Activity
            {
                RegisterStart = now,
                RegisterEnd = now.AddDays(7),
                StartTime = now.AddDays(7),
                EndTime = now.AddDays(7).AddHours(2)
            };
            return View("~/Views/Home/AdminActivityManage.cshtml", model);
        }

        [HttpPost]
        [Auth]
        [Role("admin,organizer")]
        public async Task<IActionResult> Create(
            [Bind("Title,Category,Location,Capacity,NeedAudit,RegisterStart,RegisterEnd,StartTime,EndTime,Description")]
            Activity model)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return RedirectToAction("Login", "Home");

            model.Description ??= "";

            ModelState.Remove("Organizer");
            ModelState.Remove("Registrations");
            ModelState.Remove("SignInSessions");
            ModelState.Remove("OrganizerId");

            if (model.RegisterStart >= model.RegisterEnd)
                ModelState.AddModelError("RegisterEnd", "报名结束时间必须晚于开始时间");
            if (model.RegisterEnd > model.StartTime)
                ModelState.AddModelError("RegisterEnd", "报名结束时间不能晚于活动开始时间");
            if (model.StartTime >= model.EndTime)
                ModelState.AddModelError("EndTime", "活动结束时间必须晚于开始时间");

            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = false;
                return View("~/Views/Home/AdminActivityManage.cshtml", model);
            }

            model.OrganizerId = userId;
            model.Status = "PUBLISHED";
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            _context.Activities.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "活动发布成功";
            return RedirectToAction("Index");
        }

        // =============== 编辑活动 ===============
        [HttpGet]
        [Auth]
        [Role("admin,organizer")]
        public async Task<IActionResult> Edit(long id)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return RedirectToAction("Login", "Home");
            if (userId != activity.OrganizerId)
            {
                var rolesStr = HttpContext.Session.GetString("Roles");
                if (string.IsNullOrEmpty(rolesStr) || !rolesStr.Contains("admin"))
                    return RedirectToAction("AccessDenied", "Home");
            }

            ViewBag.IsEdit = true;
            return View("~/Views/Home/AdminActivityManage.cshtml", activity);
        }

        [HttpPost]
        [Auth]
        [Role("admin,organizer")]
        public async Task<IActionResult> Edit(long id,
            [Bind("Id,Title,Category,Location,Capacity,NeedAudit,RegisterStart,RegisterEnd,StartTime,EndTime,Description")]
            Activity model)
        {
            if (id != model.Id) return NotFound();

            var activity = await _context.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return RedirectToAction("Login", "Home");
            if (userId != activity.OrganizerId)
            {
                var rolesStr = HttpContext.Session.GetString("Roles");
                if (string.IsNullOrEmpty(rolesStr) || !rolesStr.Contains("admin"))
                    return RedirectToAction("AccessDenied", "Home");
            }

            model.Description ??= "";

            ModelState.Remove("Organizer");
            ModelState.Remove("Registrations");
            ModelState.Remove("SignInSessions");
            ModelState.Remove("OrganizerId");

            if (!ModelState.IsValid)
            {
                ViewBag.IsEdit = true;
                return View("~/Views/Home/AdminActivityManage.cshtml", model);
            }

            activity.Title = model.Title;
            activity.Category = model.Category;
            activity.Location = model.Location;
            activity.Capacity = model.Capacity;
            activity.NeedAudit = model.NeedAudit;
            activity.RegisterStart = model.RegisterStart;
            activity.RegisterEnd = model.RegisterEnd;
            activity.StartTime = model.StartTime;
            activity.EndTime = model.EndTime;
            activity.Description = model.Description;
            activity.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // 通知已报名用户
            var registrations = await _context.Registrations
                .Where(r => r.ActivityId == id && (r.Status == "REGISTERED" || r.Status == "PENDING"))
                .ToListAsync();
            foreach (var reg in registrations)
                await _noticeService.SendAsync(reg.UserId, "ACTIVITY_UPDATE",
                    "活动信息更新", $"您报名的活动「{activity.Title}」信息已更新，请留意。");

            TempData["Success"] = "活动修改成功，已通知已报名用户";
            return RedirectToAction("Index");
        }

        // =============== 删除活动 ===============
        [HttpPost]
        [Auth]
        [Role("admin,organizer")]
        public async Task<IActionResult> Delete(long id)
        {
            var activity = await _context.Activities.FindAsync(id);
            if (activity == null) return NotFound();

            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return RedirectToAction("Login", "Home");
            if (userId != activity.OrganizerId)
            {
                var rolesStr = HttpContext.Session.GetString("Roles");
                if (string.IsNullOrEmpty(rolesStr) || !rolesStr.Contains("admin"))
                    return RedirectToAction("AccessDenied", "Home");
            }

            var registrations = await _context.Registrations.Where(r => r.ActivityId == id).ToListAsync();
            _context.Registrations.RemoveRange(registrations);
            _context.Activities.Remove(activity);
            await _context.SaveChangesAsync();

            TempData["Success"] = "活动已删除";
            return RedirectToAction("Index");
        }
    }
}
