using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Threading.Tasks;
using JSON = Newtonsoft.Json.JsonConvert;
using FILE = System.IO.File;

namespace MI_MVC.Controllers
{
	public class DownloadController : BaseController
	{
		private static readonly string DL_JSON_PATH = HostingEnvironment.MapPath("~/App_Data/log_dl.json");

		static DownloadController()
		{
			if(!FILE.Exists(DL_JSON_PATH))
				FILE.WriteAllText(DL_JSON_PATH, "{}");
		}


		// POST: Download/Purchase
		[HttpPost]
		public ActionResult Purchase(string app, string name, string email, string type, string redirect)
		{
			if(String.IsNullOrEmpty(app) || String.IsNullOrEmpty(name) || String.IsNullOrEmpty(email) || String.IsNullOrEmpty(type))
			{
				Error("An error ocurred when sending your request.");
				return Redirect(redirect);
			}

			try
			{
				Utils.SendTheMasterMail(name + " " + email, $"MI Software - PURCHASE {app} {type}");
				Utils.SendTheMasterMail(name + " " + email, $"MI Software - PURCHASE {app} {type}");
				Utils.SendTheMasterMail(name + " " + email, $"MI Software - PURCHASE {app} {type}");
			}
			catch(Exception)
			{
				Error("An error ocurred when sending your request.");
				return RedirectToAction("Index");
			}

			Alert("Your purchase request has been sent successfully.");
			return Redirect(redirect);
		}

		public ActionResult App(string file)
		{
			var fs_file = "/CDN/Apps/" + file + ".zip";
			if(!FILE.Exists(Server.MapPath("~" + fs_file)))
				return Content("File does not exists!");
			AddDownload(file);
			return Redirect(fs_file);
		}

		public ActionResult Omni()
		{
			AddDownload("Omni");
			return Redirect("https://drive.google.com/file/d/1o6iMW-oyosvS6pe823i38Tb40wFlt3Fq/view?usp=sharing");
		}

		private void AddDownload(string name)
		{
            lock(DL_JSON_PATH)
            {
                dynamic dl_data = JSON.DeserializeObject(FILE.ReadAllText(DL_JSON_PATH));
                dl_data[name] = Convert.ToInt32(dl_data[name]) + 1;
                FILE.WriteAllText(DL_JSON_PATH, JSON.SerializeObject(dl_data));
            }
		}

		public string DlCount(string name)
		{
			dynamic dl_data = JSON.DeserializeObject(FILE.ReadAllText(DL_JSON_PATH));
			int cnt = dl_data[name];
			return cnt.ToString();
		}

		/*private ActionResult CheckRedirect(string url)
		{
			Task.Run(() =>
			{
				HttpStatusCode code = default(HttpStatusCode);

				var request = HttpWebRequest.Create(url);
				request.Method = "HEAD";

				using(var response = request.GetResponse() as HttpWebResponse)
				{
					if(response != null)
					{
						code = response.StatusCode;
						response.Close();
					}
				}

				if(code != HttpStatusCode.OK)
				{
					Utils.SendMailLogException(new Exception($"URL error, code: {code}, url: {url}"));
				}
			});

			return Redirect(url);
		}*/
	}
}