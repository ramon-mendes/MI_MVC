using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace MI_MVC.Controllers
{
	public class SciterController : BaseController
	{
		// GET: Sciter
		public ActionResult Index()
		{
			ViewBag.sciter_files = Directory.EnumerateFiles(Server.MapPath("~/App_Data/sciter_versions"))
				.Select(sc => Path.GetFileName(sc))
				.CustomSort()
				.ToList();
			return View();
		}

		// GET: Sciter/Login
		[HttpPost]
		public ActionResult Login()
		{
			Error("Login failed");
			return RedirectToAction("Index");
		}

		// GET: Sciter/Subscribe
		public ActionResult Subscribe()
		{
			return View();
		}

		// POST: Sciter/Subscribe
		[HttpPost]
		public ActionResult Subscribe(FormCollection form)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Name: " + form["name"]);
			sb.AppendLine("E-mail: " + form["email"]);
			sb.AppendLine("Msg: " + form["msg"]);

			Utils.SendTheMasterMail(sb.ToString(), "MI Software - Subscribe Sciter Assets");

			Alert("Your request was sent successfully.");
			return RedirectToAction("Index");
		}
	}
}