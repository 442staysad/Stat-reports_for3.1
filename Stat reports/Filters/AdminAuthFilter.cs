namespace Stat_reports.Filters
{
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Filters;
    using Microsoft.AspNetCore.Http;

    public class AdminAuthFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var role = context.HttpContext.Session.GetString("UserRole");
            if (role != "Admin")
            {
                context.Result = new RedirectToActionResult("Index", "Home", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }
}
