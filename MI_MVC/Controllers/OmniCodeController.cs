using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using MI_MVC.Models;

namespace MI_MVC.Controllers
{
	public class OmniCodeController : BaseController
	{
		// GET: OmniCode
		public ActionResult Index()
		{
			ViewBag.Title  = "OmniCode";
			return View();
		}

		// GET: OmniCode/Changelog
		public ActionResult Changelog()
		{
			ViewBag.Title  = "OmniCode Changelog";
			return View();
		}

		// GET: OmniCode/Shortcuts
		public ActionResult Shortcuts()
		{
			ViewBag.Title = "OmniCode Shortcuts";
			return View();
		}

		// GET: OmniCode/EULA
		public ActionResult EULA()
		{
			string txt = System.IO.File.ReadAllText(Server.MapPath("/Views/OmniCode/EULA.md"));
			ViewBag.EULA = BlogFileSystemManager.TransformMarkdown(txt);
			ViewBag.Title  = "OmniCode EULA";
			return View();
		}

		// GET: OmniCode/Convention
		public ActionResult Convention()
		{
			string txt = System.IO.File.ReadAllText(Server.MapPath("/Views/OmniCode/Convention.md"));
			ViewBag.Title  = "OmniCode Code conventions";
			ViewBag.Mkdhtml = BlogFileSystemManager.TransformMarkdown2(txt);
			return View("MkdResult");
		}
	}
}