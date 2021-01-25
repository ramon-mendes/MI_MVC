using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;

namespace MI_MVC.Controllers
{
	public class BaseController : Controller
	{
		public void Alert(string msg)
		{
			TempData["msg-alert"] = msg;
		}
		public void Error(string msg)
		{
			TempData["msg-error"] = msg;
		}

		public ActionResult JsonData(object data)
		{
			return Content(JsonConvert.SerializeObject(data), "application/json");
		}
	}
}