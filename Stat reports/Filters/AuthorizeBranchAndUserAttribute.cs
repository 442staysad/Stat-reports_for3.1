using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Stat_reportsnt.Filters
{
    public class AuthorizeBranchAndUserAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var branchId = context.HttpContext.Session.GetInt32("BranchId");
            var userId = context.HttpContext.Session.GetInt32("UserId");

            if (branchId == null)
            {
                context.Result = new RedirectToActionResult("BranchLogin", "Auth", null);
                return;
            }

            if (userId == null)
            {
                context.Result = new RedirectToActionResult("UserLogin", "Auth", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}
