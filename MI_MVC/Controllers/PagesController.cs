using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MI_MVC.Controllers
{
    public class PagesController : Controller
    {
		// GET: /Pages/Antlr4Code
		public ActionResult Antlr4Code()
		{
			return View();
		}

		// GET: /Pages/OmniView
		public ActionResult OmniView()
		{
			return View();
		}

		// GET: /Pages/CorreiosRastreamento
		public ActionResult CorreiosRastreamento()
		{
			return View();
		}

		// GET: /Pages/Donated
		public ActionResult Donated()
		{
			return View();
		}
	}
}