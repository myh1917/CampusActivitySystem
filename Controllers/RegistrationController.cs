using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusActivitySystem.Data;
using CampusActivitySystem.Models;
using CampusActivitySystem.Services;

namespace CampusActivitySystem.Controllers
{
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
            // 从 Session 获取当前登录用户的 ID
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                TempData["Error"] = "请先登录。";
                return RedirectToAction("Index", "Activity");
            }

            using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var activity = await _context.Activities.FirstOrDefaultAsync(a => a.Id == activityId);
                if (activity == null || activity.Status != "PUBLISHED")
                    throw new Exception("活动不可报名");
                if (DateTime.Now < activity.RegisterStart || DateTime.Now > activity.RegisterEnd)
                    throw new Exception("不在报名时间范围内");
                if (await _context.Registrations.AnyAsync(r => r.ActivityId == activityId && r.UserId == userId && r.Status != "CANCELLED"))
                    throw new Exception("您已报名");
                if (activity.RegisteredCount >= activity.Capacity)
                    throw new Exception("名额已满");

                var reg = new Registration
                {
                    ActivityId = activityId,
                    UserId = userId,
                    Status = "REGISTERED",
                    RegisteredAt = DateTime.Now
                };

                _context.Registrations.Add(reg);
                activity.RegisteredCount++;
                await _context.SaveChangesAsync();
                tx.Commit();

                // 发送通知
                await _noticeService.SendAsync(userId, "REGISTER_SUCCESS", "报名成功", $"您已成功报名活动「{activity.Title}」");

                TempData["Success"] = "报名成功！";
            }
            catch (Exception ex)
            {
                tx.Rollback();
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Index", "Activity");
        }

        // 查看我的报名
        public async Task<IActionResult> MyRegistrations()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
            {
                // 未登录，返回空列表
                return View(new List<Registration>());
            }

            var regs = await _context.Registrations
                .Include(r => r.Activity)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.RegisteredAt)
                .ToListAsync();

            return View(regs);
        }
    }
}