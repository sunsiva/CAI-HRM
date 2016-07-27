using HRPortal.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace HRPortal
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
            if (System.Configuration.ConfigurationManager.AppSettings["IsServerEnv"] == "true") { 
            GlobalFilters.Filters.Add(new RequireHttpsAttribute());
            }
        }

       protected void Session_Start(object sender, EventArgs e)
        {
            // Code that runs when a new session is started
            Session.Timeout = 1440;
        }

        protected void Session_End(object sender, EventArgs e)
        {
            LoginViewModel loginVM = new LoginViewModel();
            loginVM.UserLogs(false, HttpContext.Current.User.Identity.Name, "Session Timeout");
        }
    }
}
