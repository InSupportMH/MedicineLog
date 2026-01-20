using MedicineLog.Application.Terminals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MedicineLog.Infrastructure.Auth
{
    public sealed class RequirePairingAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var terminalCtx = context.HttpContext.RequestServices.GetRequiredService<ITerminalContextAccessor>();
            if (terminalCtx.Current is null)
            {
                context.Result = new RedirectToActionResult("Pair", "Terminal", routeValues: new { area = "kiosk" });
            }
        }
    }
}
