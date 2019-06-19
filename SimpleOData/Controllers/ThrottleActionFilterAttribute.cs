using Microsoft.AspNetCore.Mvc.Filters;
using System;
using System.Threading;

namespace SimpleOData.Controllers
{
    /// <summary>
    /// Attribute to simulate slow connection using ManagerController
    /// </summary>
    public class ThrottleActionFilterAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if(!Equals(ManagerController.throttleDelaySecs, 0))
              Thread.Sleep(TimeSpan.FromSeconds(ManagerController.throttleDelaySecs));
        }
    }
}
