using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Hosting;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JSON = Newtonsoft.Json.JsonConvert;
using FILE = System.IO.File;

namespace MI_MVC.Controllers
{
	public class BootstrapController : BaseController
	{
		private static readonly string JSON_PATH_LOG = HostingEnvironment.MapPath("~/App_Data/bootstrap_log.json");
		private static readonly string JSON_PATH_COMMIT = HostingEnvironment.MapPath("~/App_Data/bootstrap_commit.json");

		// GET: Bootstrap
		public ActionResult Index()
		{
			ViewBag.page = "Index";
			ViewBag.Title = "Sciter Bootstrap";
			return View();
		}

		// GET: Bootstrap
		public ActionResult Dev()
		{
			ViewBag.page = "Dev";
			ViewBag.Title = "Sciter Bootstrap Dev";
			return View();
		}

		// GET: Bootstrap
		public ActionResult Templates()
		{
			ViewBag.page = "Templates";
			ViewBag.Title = "Sciter Bootstrap Templates";
			return View();
		}

		// GET: Bootstrap
		public ActionResult Download()
		{
			ViewBag.Title = "Sciter Bootstrap Download";
			ViewBag.page = "Download";
			ViewBag.commit = FILE.ReadAllText(JSON_PATH_COMMIT);
			return View();
		}

		// POST: Bootstrap/DoBuild
		[HttpPost]
		public ActionResult DoBuild(string combination, string template, string title)
		{
			if(string.IsNullOrEmpty(title))
			{
				Error("Invalid project title");
				return RedirectToAction("Download");
			}

			// LOG
			{
				if(!FILE.Exists(JSON_PATH_LOG))
					FILE.WriteAllText(JSON_PATH_LOG, "{ count: 0, what: [] }");

				dynamic log_data = JSON.DeserializeObject(FILE.ReadAllText(JSON_PATH_LOG));
				log_data["count"] = Convert.ToInt32(log_data["count"]) + 1;

				List<string> list = log_data["what"].ToObject<List<string>>();
				list.Add(combination + "/" + template);
				log_data["what"] = JToken.FromObject(list);

				FILE.WriteAllText(JSON_PATH_LOG, JSON.SerializeObject(log_data, Formatting.Indented));
			}

			switch(combination)
			{
				case "CROSS":
					return BuildPackage("CS-TemplateMultiPlatform", title);

				case "WINFORMS":
					return BuildPackage("CS-WinForms", title);

				case "WPF":
					return BuildPackage("CS-WPF", title);

				case "CSNATIVE":
					if(template == "NONE")
						return BuildPackage("CS-default", title);
					else if(template == "AEROTABS")
						return BuildPackage("CS-TemplateAeroTabs", title);
					else if(template == "WEBCAM.AFORGE")
						return BuildPackage("CS-TemplateWebcam", title);
					else if(template == "NCRENDERER")
						return BuildPackage("CS-TemplateNCRenderer", title);
					else if(template == "GADGETS")
						return BuildPackage("CS-TemplateDesktopGadgets", title);
					else if(template == "SIDEBAR")
						return BuildPackage("CS-TemplateDesktopSidebar", title);
					break;

				case "CPP":
					return BuildPackage("CPP-Multi", title);
					//if(template == "WEBCAM.OPENCV")
					//	return BuildPackage("CPP-TemplateOpenCV", title);

				case "D":
					if(template == "AEROTABS")
						return BuildPackage("D-TemplateAeroTabs", title);
					return BuildPackage("D-default", title);

				case "PYTHON":
					return RedirectToAction("Download");
			}
			return RedirectToAction("Download");
		}

		class MyEncoder : UTF8Encoding
		{
			public override byte[] GetBytes(string s)
			{
				s = s.Replace("\\", "/");
				return base.GetBytes(s);
			}
		}

		private FilePathResult BuildPackage(string subdir, string title)
		{
			// copy source to tmp dir
			string tmpdir = Path.GetTempPath() + Guid.NewGuid().ToString() + '\\' + subdir;
			{
				Directory.CreateDirectory(tmpdir);

				string dir = Server.MapPath("~/App_Data/Cache/BootstrapCache/SciterBootstrap-master/") + subdir;
				Utils.DirectoryCopy(dir, tmpdir, true);

#if DEBUG
				//Process.Start("explorer", tmpdir);
#endif
			}
			
			// adjust things for our user
			{
				// remove trash
				FILE.Delete(tmpdir + "\\appveyor.yml");
				FILE.Delete(tmpdir + "\\README.md");

				// rename things
				var dirs = Directory.EnumerateDirectories(tmpdir, "SciterBootstrap", SearchOption.AllDirectories).Reverse().ToList();
				foreach(var path in dirs)
				{
					string newdir = Path.GetDirectoryName(path) + "\\" + title;
					Directory.Move(path, newdir);
				}

				if(subdir == "CPP-Multi")
				{
					string dir = Directory.EnumerateDirectories(tmpdir, "SciterBootstrap.xcodeproj", SearchOption.AllDirectories).Single();
					string newdir = Path.GetDirectoryName(dir) + "\\" + title + ".xcodeproj";
					Directory.Move(dir, newdir);
				}

				var files1 = Directory.EnumerateFiles(tmpdir, "*SciterBootstrap*.*", SearchOption.AllDirectories).ToList();
				foreach(var path in files1)
				{
					string newname = Path.GetDirectoryName(path) + "\\" + Path.GetFileName(path).Replace("SciterBootstrap", title);
					FILE.Move(path, newname);
				}

				var files2 = Directory.EnumerateFiles(tmpdir, "*", SearchOption.AllDirectories).ToList();
				string[] BIN_EXTS = new string[] { ".ico", ".png", ".bmp", ".gif", ".jpg", ".jpeg", ".exe", ".dll", ".so", ".dylib", ".lib", ".pdb", ".zip" };
				foreach(var path in files2)
				{
					var ext = Path.GetExtension(path);
					if(BIN_EXTS.Contains(ext))
						continue;
					if(path.EndsWith("packfolder"))
						continue;

					string text = FILE.ReadAllText(path);
					text = text.Replace("SciterBootstrap", title);
					FILE.WriteAllText(path, text);
				}
			}

			// pack as ZIP
			string zippath;
			{
				zippath = Path.GetTempFileName();
				if(FILE.Exists(zippath))
					FILE.Delete(zippath);

				string dirup = tmpdir.Substring(0, tmpdir.Length - subdir.Length);
				ZipFile.CreateFromDirectory(dirup, zippath, CompressionLevel.Fastest, false, new MyEncoder());
			}

			return File(zippath, "application/zip", title + ".zip");
		}


		// POST: /Bootstrap/CacheGithubRepo
		private static object _cacheLock = new object();

		//[HttpPost] so I can GET in browser
		public string CacheGithubRepo()
		{
			if(!Monitor.TryEnter(_cacheLock))
				throw new Exception("CacheGithubRepo already running");

			string msg;
			try
			{
				Utils.SendTheMasterMail("", "CacheGithubRepo STARTED");

				using(var client = new TimedWebClient())
				{
					// PAYLOAD
					// Get commit message/date
					string json;

					client.Headers.Add("User-Agent: MISoftware");// have to add for every request
					json = client.DownloadString("https://api.github.com/repos/ramon-mendes/SciterBootstrap/git/refs/heads/master");
					dynamic json_commit = JSON.DeserializeObject(json);
					string commit_url = json_commit.@object.url;

					client.Headers.Add("User-Agent: MISoftware");// have to add for every request
					json = client.DownloadString(commit_url);
					dynamic json_message = JSON.DeserializeObject(json);

					string message = json_message.message;
					DateTime date = DateTime.Parse(json_message.committer.date.ToString());

					msg = $"{message} on {date}";
					FILE.WriteAllText(JSON_PATH_COMMIT, msg);

					// Get ZIP
					Task.Run(() =>
					{
						string zippath = Path.GetTempFileName();
						client.DownloadFile("https://github.com/ramon-mendes/SciterBootstrap/archive/master.zip", zippath);

						string cachedir = Server.MapPath("~/App_Data/Cache/BootstrapCache");
						string tmpdir = Server.MapPath("~/App_Data/Cache/BootstrapCache2");
						if(Directory.Exists(tmpdir))
							Directory.Delete(tmpdir, true);

						using(var zip = ZipFile.Open(zippath, ZipArchiveMode.Read))
						{
							zip.ExtractToDirectory(tmpdir);
						}

						if(Directory.Exists(cachedir))
							Directory.Delete(cachedir, true);
						Directory.Move(tmpdir, cachedir);

						Utils.SendTheMasterMail("", "CacheGithubRepo DONE");
					});
				}
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
				throw;
			}
			finally
			{
				Monitor.Exit(_cacheLock);
			}

			return "SUCCESS, commit msg: " + msg;
		}
		

		public string Debug()
		{
			string path = System.IO.Path.GetTempPath() + Guid.NewGuid().ToString();
			Directory.CreateDirectory(path);

			return "res:" + path;
		}
	}
}