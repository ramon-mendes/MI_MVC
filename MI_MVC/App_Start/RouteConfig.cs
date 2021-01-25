using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace MI_MVC
{
	public class RouteConfig
	{
		public static void RegisterRoutes(RouteCollection routes)
		{
			routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

			routes.MapRoute(
				name: "Post",
				url: "Home/Post/{slug}",
				defaults: new { controller = "Home", action = "Post" }
			);

			routes.MapRoute(
				name: "Tag",
				url: "Home/Tag/{tag}",
				defaults: new { controller = "Home", action = "Tag" }
			);

			routes.MapRoute(
				name: "DlApp",
				url: "Download/App/{file}",
				defaults: new { controller = "Download", action = "App" }
			);

			routes.MapRoute(
				name: "Default",
				url: "{controller}/{action}/{id}",
				defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional }
			);

			routes.IgnoreRoute("{file}.js");
			routes.IgnoreRoute("{file}.html");
		}
	}
}