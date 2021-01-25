using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using MI_MVC.Models;

namespace MI_MVC.Controllers
{
	public class OmniController : BaseController
	{
		// GET: Omni
		public ActionResult Index()
		{
			ViewBag.Title = "Omni";
			return View();
		}

		// GET: Omni/Changelog
		public ActionResult Changelog()
		{
			ViewBag.Title = "Omni Changelog";
			return View();
		}

		// GET: Omni/EULA
		public ActionResult EULA()
		{
			string txt = System.IO.File.ReadAllText(Server.MapPath("/Views/Omni/EULA.md"));
			ViewBag.EULA = BlogFileSystemManager.TransformMarkdown(txt);
			ViewBag.Title  = "Omni EULA";
			return View();
		}
	}
}