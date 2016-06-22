﻿using System;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace HRPortal.Common
{
    public class LogActionFilter : ActionFilterAttribute

    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (!HttpContext.Current.User.Identity.IsAuthenticated)
            {
                filterContext.Result = new RedirectToRouteResult(
                new RouteValueDictionary
                    {{"controller", "Account"}, {"action", "Login"}});
            }

            //TODO: Log Action Filter Call: And store it to DB.
            //MusicStoreEntities storeDB = new MusicStoreEntities();
            //ActionLog log = new ActionLog()
            //{
            //    Controller = filterContext.ActionDescriptor.ControllerDescriptor.ControllerName,
            //    Action = filterContext.ActionDescriptor.ActionName,
            // IP = filterContext.HttpContext.Request.UserHostAddress,
            // DateTime = filterContext.HttpContext.Timestamp
            //};
            //storeDB.ActionLogs.Add(log);
            //storeDB.SaveChanges();
            
            Log("OnActionExecuting", filterContext.RouteData);
        }

        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            Log("OnActionExecuted", filterContext.RouteData);
        }

        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            Log("OnResultExecuting", filterContext.RouteData);
        }

        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            Log("OnResultExecuted", filterContext.RouteData);
        }


        private void Log(string methodName, RouteData routeData)
        {
            var controllerName = routeData.Values["controller"];
            var actionName = routeData.Values["action"];
            var message = String.Format("{0} controller:{1} action:{2}", methodName, controllerName, actionName);
            Debug.WriteLine(message, "Action Filter Log");
        }

    }
}