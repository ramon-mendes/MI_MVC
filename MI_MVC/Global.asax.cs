using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using MI_MVC.Controllers;
using MI_MVC.DAL;

namespace MI_MVC
{
	public class MvcApplication : HttpApplication
	{
		protected void Application_Start()
		{
			ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

			CultureInfo.DefaultThreadCurrentCulture = new CultureInfo("en-US");
			Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
			Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

			//AreaRegistration.RegisterAllAreas();
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
			//GlobalConfiguration.Configure(WebApiConfig.Register);

			new EstacaoContext().Database.Initialize(false);
			new ChegueiContext().Database.Initialize(false);
			new EnergiaContext().Database.Initialize(false);

			//if(!Utils.IsLocalHost)
			//	WeatherController.Setup();

			//if(Utils.IsAzure)
				CronjobsController.Setup();

			Console.WriteLine("AQUIIIII!");
			Debug.WriteLine("AQUIIIII!!");
		}
	}
}