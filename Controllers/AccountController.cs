using CampusActivitySystem.Data;
using CampusActivitySystem.Filters;
using CampusActivitySystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
public class AccountController : Controller
{
    private readonly AppDbContext _context;
    public AccountController(AppDbContext context) { _context = context; }
    // GET: 注册页面
    public IActionResult Register() => View();
    // POST: 处理注册
    [HttpPost]
    public async Task<IActionResult> Register(string account, string password, string name, string studentNo)
    {
        if (await _context.Users.AnyAsync(u => u.Account == account))
        {
            ModelState.AddModelError("", "账号已存在");
            return View("~/Views/Home/Register.cshtml");
        }
        var user = new User
        {
            Account = account,
            PasswordHash = HashPassword(password),
            Name = name,
            StudentNo = studentNo,
            Status = "ACTIVE",
            College = "未填写",
            Phone=""
        };
        _context.Users.Add(user);
        // 默认给 student 角色
        var studentRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "student");
        if (studentRole != null)
            _context.UserRoles.Add(new UserRole { User = user, Role = studentRole });

        await _context.SaveChangesAsync();
        TempData["RegisterSuccess"] = "注册成功！请登录您的账号";
        return RedirectToAction("Login","Home");
    }
    private string HashPassword(string raw)
    {
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToBase64String(bytes);
    }

    public IActionResult Login() => View();
    //登录
    [HttpPost]
    public async Task<IActionResult> Login(string account, string password)
    {
        var hashed = HashPassword(password);

        // 查询用户，同时把角色带出来
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Account == account && u.PasswordHash == hashed);

        if (user == null || user.Status != "ACTIVE")
        {
            ModelState.AddModelError("", "账号或密码错误");
            return View("~/Views/Home/Login.cshtml");
        }

        // 1. 保存用户基本信息到 Session
        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("UserName", user.Name);

        // 2. 保存角色列表到 Session
        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        HttpContext.Session.SetString("Roles", string.Join(",", roles));

        return RedirectToAction("Index", "Home");
    }
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    // GET: 个人信息页面
    [Auth]
    public async Task<IActionResult> Info()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Home");
        }

        var user = await _context.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == long.Parse(userId));

        if (user == null)
        {
            return NotFound();
        }

        return View("~/Views/Home/Info.cshtml", user);
    }

    // POST: 更新个人信息
    [HttpPost]
    [Auth]
    public async Task<IActionResult> Info(string name, string phone, string college)
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToAction("Login", "Home");
        }

        var user = await _context.Users.FindAsync(long.Parse(userId));
        if (user == null)
        {
            return NotFound();
        }

        // 更新字段
        user.Name = name ?? user.Name;
        user.Phone = phone ?? user.Phone;
        user.College = college ?? user.College;
        user.UpdatedAt = DateTime.Now;

        await _context.SaveChangesAsync();

        // 更新 Session 中的用户名
        HttpContext.Session.SetString("UserName", user.Name);

        TempData["SuccessMessage"] = "个人信息更新成功！";
        return RedirectToAction("Info");
    }

}

