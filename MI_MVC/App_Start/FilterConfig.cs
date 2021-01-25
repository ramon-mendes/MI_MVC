using System.Web;
using System.Web.Mvc;
using MI_MVC.Classes;

namespace MI_MVC
{
	public class FilterConfig
	{
		public static void RegisterGlobalFilters(GlobalFilterCollection filters)
		{
			filters.Add(new HandleErrorAttribute());
		}
	}
}
