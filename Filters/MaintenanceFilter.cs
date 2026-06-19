using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using CampusActivitySystem.Data;

namespace CampusActivitySystem.Filters
{
    public class MaintenanceFilter : IAsyncActionFilter
    {
        private readonly AppDbContext _context;

        public MaintenanceFilter(AppDbContext context)
        {
            _context = context;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var path = context.HttpContext.Request.Path.Value?.ToLower() ?? "";

            // 静态资源直接放行
            if (path.StartsWith("/css/") ||
                path.StartsWith("/js/") ||
                path.StartsWith("/lib/") ||
                path.StartsWith("/_framework/"))
            {
                await next();
                return;
            }

            // 始终允许访问的页面：维护页、登录、退出
            if (path.StartsWith("/home/maintenance") ||
                path.StartsWith("/account/login") ||
                path.StartsWith("/account/logout"))
            {
                await next();
                return;
            }

            // 检查维护模式
            var maintenance = await _context.SystemConfigs
                .FirstOrDefaultAsync(c => c.ConfigKey == "MaintenanceMode");

            if (maintenance?.ConfigValue == "true")
            {
                // 判断是否为管理员
                var roles = context.HttpContext.Session.GetString("Roles") ?? "";
                bool isAdmin = roles.Contains("admin");

                if (!isAdmin)
                {
                    // 非管理员 → 重定向到维护页
                    context.Result = new RedirectResult("/Home/Maintenance");
                    return;
                }
            }

            await next();
        }
    }
}