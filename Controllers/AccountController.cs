using CampusActivitySystem.Data;
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
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Account == account && u.PasswordHash == hashed);
        if (user == null || user.Status != "ACTIVE")
        {
            ModelState.AddModelError("", "账号或密码错误");
            return View("~/Views/Home/Login.cshtml");
        }
        // 写入 Session
        HttpContext.Session.SetString("UserId", user.Id.ToString());
        HttpContext.Session.SetString("UserName", user.Name);
        // 还可以保存角色列表，但可以每次从数据库查，简单处理
        return RedirectToAction("Index", "Home");
    }
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Index", "Home");
    }

    
}

