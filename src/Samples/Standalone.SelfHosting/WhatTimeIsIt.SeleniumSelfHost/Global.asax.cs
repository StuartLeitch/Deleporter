using System;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace WhatTimeIsIt.SeleniumSelfHost
{
    public class MvcApplication : HttpApplication
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            RegisterRoutes(RouteTable.Routes);
            ControllerBuilder.Current.SetControllerFactory(new NinjectControllerFactory());

            //var temp = new DeleporterCore.Server.DeleporterServerModule();
            //temp.OnInit();

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            //var temp = new DeleporterCore.Server.DeleporterServerModule();
            //temp.Context_BeginRequest(sender, e);

        }

    }
}