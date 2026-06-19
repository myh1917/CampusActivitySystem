using Microsoft.AspNetCore.Mvc;
using CampusActivitySystem.Models;

namespace CampusActivitySystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
    }

    public IActionResult Logout()
    {
        HttpContext.Session.Clear();   // 清除所有 Session 数据
        return RedirectToAction("Index", "Home");
    }

    public IActionResult MyActivities()
    {
        return View();
    }
    
    public IActionResult AccessDenied() //访问活动管理失败
    {
        return View();
    }

    // ============ 预览用Action，正式开发后可以删除 ============
    public IActionResult Login()
    {
        return View();
    }
    public IActionResult Register()
    {
        return View();
    }
    public IActionResult ActivityList()
    {
        return View();
    }
  /*  public IActionResult ActivityDetail()
    {
        return View(); // 后期会接收id参数，现在先预览
    }*/
    public IActionResult SignIn()
    {
        return View();
    }
    // ============ 预览用Action（全部页面）============
    public IActionResult ActivityDetail() => View();
    public IActionResult Profile() => View();           // 个人信息
    public IActionResult MyRegistrations() => View();   // 我的报名
    public IActionResult AdminIndex() => View();        // 后台首页
    //public IActionResult AdminUserList() => View();     // 用户管理
    public IActionResult AdminActivityManage() => View();// 活动管理
    public IActionResult SignControl() => View();       // 签到控制台
    public IActionResult Statistics() => View();        // 统计导出
    public IActionResult Notices() => View();           // 通知中心
    public IActionResult FakeLogin()
    {
        HttpContext.Session.SetInt32("UserId", 2); // 假设学生ID=2，请确保数据库中有该用户
        return RedirectToAction("Index", "Activity");
    }

}
