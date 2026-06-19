using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
namespace CampusActivitySystem.Filters
{
    //[Auth] 表示需要登录，[Role("admin")] 表示需要管理员。
    public class AuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
            base.OnActionExecuting(context);
        }
    }
    public class RoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _role;
        public RoleAttribute(string role) { _role = role; }
        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }
            // 检查角色（简化：我们可以从 Session 里再存个 Role，或者查数据库，这里为简单，我们直接查一次）
            var db = context.HttpContext.RequestServices.GetService<Data.AppDbContext>();
            var user = db.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role).FirstOrDefault(u => u.Id == long.Parse(userId));
            var hasRole = user?.UserRoles.Any(ur => ur.Role.Code == _role);
            if (!hasRole.HasValue || !hasRole.Value)
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
            }
        }
    }
}
