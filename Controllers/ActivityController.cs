using CampusActivitySystem.Data;
using CampusActivitySystem.Filters;
using CampusActivitySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CampusActivitySystem.Controllers
{
    [Auth]
    public class ActivityController : Controller
    {
        private readonly AppDbContext _context;

        public ActivityController(AppDbContext context)
        {
            _context = context;
        }

        // 活动列表
        public async Task<IActionResult> Index(string keyword, string category)
        {
            var query = _context.Activities.Where(a => a.Status != "CANCELLED");

            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(a => a.Title.Contains(keyword));
            if (!string.IsNullOrEmpty(category))
                query = query.Where(a => a.Category == category);

            var list = await query.OrderByDescending(a => a.StartTime).ToListAsync();
            return View(list);
        }

        // 发布活动页面
        [HttpGet]
        [Role("admin")]
        public IActionResult Create()
        {
            return View("~/Views/Home/AdminActivityManage.cshtml", new Activity());
        }

        // 处理发布
        [HttpPost]
        [Role("admin")]
        public async Task<IActionResult> Create(
            [Bind("Title,Category,Location,Capacity,NeedAudit,RegisterStart,RegisterEnd,StartTime,EndTime,Description")]
            Activity model)
        {
            // 确保描述不为 null（数据库要求非空）
            model.Description ??= "";

            // *** 关键：移除导航属性的验证错误，它们由代码赋值，不需要用户输入 ***
            ModelState.Remove("Organizer");
            ModelState.Remove("Registrations");
            ModelState.Remove("SignInSessions");
            // 如果 OrganizerId 也被错误标记，也可以移除（它会在下面赋值）
            ModelState.Remove("OrganizerId");

            // 查找 admin 用户
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Account == "admin");
            if (adminUser == null)
            {
                ModelState.AddModelError("", "系统错误：找不到管理员账户，请重新运行生成种子数据。");
                return View("~/Views/Home/AdminActivityManage.cshtml", model);
            }
            long userId = adminUser.Id;

            // 时间逻辑校验
            if (model.RegisterStart >= model.RegisterEnd)
                ModelState.AddModelError("RegisterEnd", "报名结束时间必须晚于开始时间");
            if (model.RegisterEnd > model.StartTime)
                ModelState.AddModelError("RegisterEnd", "报名结束时间不能晚于活动开始时间");
            if (model.StartTime >= model.EndTime)
                ModelState.AddModelError("EndTime", "活动结束时间必须晚于开始时间");

            // 再次检查模型状态（现在只检查用户输入的字段）
            if (!ModelState.IsValid)
                return View("~/Views/Home/AdminActivityManage.cshtml", model);

            model.OrganizerId = userId;
            model.Status = "PUBLISHED";
            model.CreatedAt = DateTime.Now;
            model.UpdatedAt = DateTime.Now;

            _context.Activities.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }
    }
}