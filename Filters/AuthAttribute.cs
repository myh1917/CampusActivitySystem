using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CampusActivitySystem.Filters
{
    // [Auth]：必须登录才能访问
    public class AuthAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var userId = context.HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new RedirectToActionResult("Login", "Home", null);
            }
            base.OnActionExecuting(context);
        }
    }

    // [Role("admin")]：必须有指定角色才能访问
    public class RoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string _role;

        public RoleAttribute(string role)
        {
            _role = role;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new RedirectToActionResult("Login", "Home", null);
                return;
            }

            // 从 Session 读取角色列表
            var rolesStr = context.HttpContext.Session.GetString("Roles");
            var roles = string.IsNullOrEmpty(rolesStr)
                ? Array.Empty<string>()
                : rolesStr.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (!roles.Contains(_role))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
            }
        }
    }
}