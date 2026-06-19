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

    // [Role("admin,organizer")]：允许指定角色（用逗号分隔）
    public class RoleAttribute : Attribute, IAuthorizationFilter
    {
        private readonly string[] _roles;

        public RoleAttribute(string roles)
        {
            _roles = roles.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            var userId = context.HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
            {
                context.Result = new RedirectToActionResult("Login", "Home", null);
                return;
            }

            var rolesStr = context.HttpContext.Session.GetString("Roles");
            if (string.IsNullOrEmpty(rolesStr))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
                return;
            }

            var userRoles = rolesStr.Split(',', StringSplitOptions.RemoveEmptyEntries);
            if (!userRoles.Any(r => _roles.Contains(r)))
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Home", null);
            }
        }
    }
}