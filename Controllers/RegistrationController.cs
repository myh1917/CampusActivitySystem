using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusActivitySystem.Data;
using CampusActivitySystem.Models;
using CampusActivitySystem.Services;
using CampusActivitySystem.Filters;

namespace CampusActivitySystem.Controllers
{
    [Auth]
    public class RegistrationController : Controller
    {
        private readonly AppDbContext _context;
        private readonly NoticeService _noticeService;

        public RegistrationController(AppDbContext context, NoticeService noticeService)
        {
            _context = context;
            _noticeService = noticeService;
        }

        // 报名活动
        [HttpPost]
        public async Task<IActionResult> Register(long activityId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                TempData["Error"] = "请先登录。";
                return RedirectToAction("Index", "Activity");
            }

            var activity = await _context.Activities.FirstOrDefaultAsync(a => a.Id == activityId);
            if (activity == null || activity.Status != "PUBLISHED")
            {
                TempData["Error"] = "活动不可报名";
                return RedirectToAction("Index", "Activity");
            }
            if (DateTime.Now < activity.RegisterStart || DateTime.Now > activity.RegisterEnd)
            {
                TempData["Error"] = "不在报名时间范围内";
                return RedirectToAction("Index", "Activity");
            }

            // 查找该用户对该活动的任意状态报名记录
            var existingReg = await _context.Registrations
                .FirstOrDefaultAsync(r => r.ActivityId == activityId && r.UserId == userId);

            if (existingReg != null)
            {
                if (existingReg.Status == "REGISTERED" || existingReg.Status == "PENDING")
                {
                    TempData["Error"] = "您已报名或正在审核中";
                    return RedirectToAction("Index", "Activity");
                }
                else if (existingReg.Status == "CANCELLED" || existingReg.Status == "REJECTED")
                {
                    // 重新报名：检查名额（如果不需要审核）
                    if (!activity.NeedAudit && activity.RegisteredCount >= activity.Capacity)
                    {
                        TempData["Error"] = "名额已满，无法重新报名";
                        return RedirectToAction("Index", "Activity");
                    }

                    existingReg.Status = activity.NeedAudit ? "PENDING" : "REGISTERED";
                    existingReg.RegisteredAt = DateTime.Now;
                    existingReg.CancelledAt = null;
                    existingReg.AuditComment = "";

                    if (!activity.NeedAudit)
                        activity.RegisteredCount++;

                    await _context.SaveChangesAsync();

                    string msg = activity.NeedAudit ? "报名已重新提交，等待审核" : "报名成功！";
                    TempData["Success"] = msg;
                    await _noticeService.SendAsync(userId, "REGISTER", "重新报名", $"您已重新报名活动「{activity.Title}」");
                    return RedirectToAction("Index", "Activity");
                }
            }

            // 全新报名
            if (!activity.NeedAudit && activity.RegisteredCount >= activity.Capacity)
            {
                TempData["Error"] = "名额已满";
                return RedirectToAction("Index", "Activity");
            }

            string initStatus = activity.NeedAudit ? "PENDING" : "REGISTERED";

            var reg = new Registration
            {
                ActivityId = activityId,
                UserId = userId,
                Status = initStatus,
                RegisteredAt = DateTime.Now,
                FormData = "",
                AuditComment = ""
            };

            _context.Registrations.Add(reg);
            if (!activity.NeedAudit)
                activity.RegisteredCount++;

            await _context.SaveChangesAsync();

            TempData["Success"] = activity.NeedAudit ? "报名已提交，等待审核" : "报名成功！";
            await _noticeService.SendAsync(userId, "REGISTER", "报名申请",
                $"您已报名活动「{activity.Title}」{(activity.NeedAudit ? "，等待审核" : "")}");

            return RedirectToAction("Index", "Activity");
        }

        // 取消报名
        [HttpPost]
        public async Task<IActionResult> Cancel(long registrationId)
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return RedirectToAction("Login", "Home");

            var reg = await _context.Registrations
                .Include(r => r.Activity)
                .FirstOrDefaultAsync(r => r.Id == registrationId && r.UserId == userId);

            if (reg == null)
            {
                TempData["Error"] = "报名记录不存在";
                return RedirectToAction("MyRegistrations");
            }

            if (reg.Status == "REGISTERED")
            {
                reg.Status = "CANCELLED";
                reg.CancelledAt = DateTime.Now;
                if (reg.Activity != null && reg.Activity.RegisteredCount > 0)
                    reg.Activity.RegisteredCount--;
                await _context.SaveChangesAsync();
                TempData["Success"] = "已取消报名";
            }
            else if (reg.Status == "PENDING")
            {
                reg.Status = "CANCELLED";
                reg.CancelledAt = DateTime.Now;
                await _context.SaveChangesAsync();
                TempData["Success"] = "已取消待审核报名";
            }
            else
            {
                TempData["Error"] = "当前状态不允许取消";
            }

            return RedirectToAction("MyRegistrations");
        }

        // 我的报名
        public async Task<IActionResult> MyRegistrations()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return RedirectToAction("Login", "Home");

            var regs = await _context.Registrations
                .Include(r => r.Activity)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RegisteredAt)
                .ToListAsync();

            return View("~/Views/Home/MyRegistrations.cshtml", regs);
        }

        // ========== 审核相关 ==========
        [HttpGet]
        [Role("admin,organizer")]
        public async Task<IActionResult> AuditList()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            long userId = long.TryParse(userIdStr, out var id) ? id : 0;
            var rolesStr = HttpContext.Session.GetString("Roles");

            IQueryable<Registration> query = _context.Registrations
                .Include(r => r.Activity)
                .Include(r => r.User)
                .Where(r => r.Status == "PENDING");

            if (rolesStr != null && rolesStr.Contains("organizer") && !rolesStr.Contains("admin"))
                query = query.Where(r => r.Activity.OrganizerId == userId);

            var list = await query.OrderByDescending(r => r.RegisteredAt).ToListAsync();
            return View("~/Views/Home/AuditList.cshtml", list);
        }

        [HttpPost]
        [Role("admin,organizer")]
        public async Task<IActionResult> Approve(long registrationId)
        {
            var reg = await _context.Registrations
                .Include(r => r.Activity)
                .FirstOrDefaultAsync(r => r.Id == registrationId);
            if (reg == null || reg.Status != "PENDING") return NotFound();

            reg.Status = "REGISTERED";
            reg.Activity.RegisteredCount++;
            await _context.SaveChangesAsync();

            await _noticeService.SendAsync(reg.UserId, "AUDIT", "报名审核通过", $"您报名活动「{reg.Activity.Title}」已审核通过");
            TempData["Success"] = "已通过";
            return RedirectToAction("AuditList");
        }

        [HttpPost]
        [Role("admin,organizer")]
        public async Task<IActionResult> Reject(long registrationId)
        {
            var reg = await _context.Registrations
                .Include(r => r.Activity)
                .FirstOrDefaultAsync(r => r.Id == registrationId);
            if (reg == null || reg.Status != "PENDING") return NotFound();

            reg.Status = "REJECTED";
            reg.AuditComment = "管理员拒绝";
            await _context.SaveChangesAsync();

            await _noticeService.SendAsync(reg.UserId, "AUDIT", "报名审核未通过", $"您报名活动「{reg.Activity.Title}」审核未通过");
            TempData["Success"] = "已拒绝";
            return RedirectToAction("AuditList");
        }
    }
}