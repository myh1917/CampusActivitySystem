using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CampusActivitySystem.Data;

namespace CampusActivitySystem.Controllers
{
    public class NoticeController : Controller
    {
        private readonly AppDbContext _context;

        public NoticeController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var userIdStr = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userIdStr) || !long.TryParse(userIdStr, out long userId))
                return RedirectToAction("Login", "Home");

            // 将该用户所有未读通知标记为已读
            var unreadNotices = await _context.Notices
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            foreach (var notice in unreadNotices)
            {
                notice.IsRead = true;
            }
            if (unreadNotices.Any())
                await _context.SaveChangesAsync();

            // 查询所有通知（包括现在已读的）
            var notices = await _context.Notices
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notices);
        }
    }
}