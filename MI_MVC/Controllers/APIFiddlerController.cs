#if false
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using DIR = System.IO.Directory;
using FILE = System.IO.File;
using JSON = Newtonsoft.Json.JsonConvert;

namespace MI_MVC.Controllers
{
	public class APIFiddlerController : BaseController
	{
		private class PublicFiddle
		{
			public string code;
			public string title;
			public string user_html;
			public string user_script;
			public string user_css;
			public string user_unittests;
		}

		private class PrivateFiddle : PublicFiddle
		{
			public string hostname;
			public DateTime dt;
		}

		private static Dictionary<string, DateTime> _anti_DoS = new Dictionary<string, DateTime>();
		private static readonly Regex _code_regex = new Regex("^[a-zA-Z0-9]+$");

		private void AntiDoS()
		{
			if(!Utils.IsLocalHost && !Request.IsSecureConnection)
			{
				throw new HttpException("Insecure");
			}

			if(_anti_DoS.ContainsKey(Request.UserHostAddress))
			{
				DateTime last_request = _anti_DoS[Request.UserHostAddress];

				if((DateTime.Now - last_request).TotalSeconds < 3)
					throw new HttpException("Slow down");
			}
			_anti_DoS[Request.UserHostAddress] = DateTime.Now;
		}

		// POST: /APIFiddler/CI_Upload
		[HttpPost]
		public void CI_Upload(string ver, string date)
		{
			try
			{
				string dirpath = Server.MapPath("~/CDN/Apps/OmniFiddler/");
				if(DIR.Exists(dirpath))
					DIR.Delete(dirpath, true);
				DIR.CreateDirectory(dirpath);
			
				byte[] data = Utils.ReadFully(Request.InputStream);
				FILE.WriteAllBytes(dirpath + "OmniFiddler-BleedingEdge.zip", data);

				FILE.WriteAllText(dirpath + "ci_build.json", JSON.SerializeObject(new
				{
					ver = ver,
					date = date
				}));
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
			}
		}

		// GET: /APIFiddler/CI_Version
		public ActionResult CI_Version()
		{
			string dirpath = Server.MapPath("~/CDN/Apps/OmniFiddler/");
			return Content(FILE.ReadAllText(dirpath + "ci_build.json"), "application/json");
		}

		// POST: /APIFiddler/FiddlerNew
		[HttpPost]
		public ActionResult FiddlerNew()
		{
			AntiDoS();

			NameValueCollection formdata = Request.Form;

			string newcode;
			string newpath;
			int hash_length = 5;
			do
			{
				newcode = Utils.RandomString(hash_length);
				newpath = Server.MapPath("~/App_Data/Fiddler/PrivateUserFiddles/" + newcode + ".json");
				hash_length++;
			} while(System.IO.File.Exists(newpath));

			var json = JSON.SerializeObject(new
			{
				title = formdata["title"],
				user_html = formdata["user_html"],
				user_script = formdata["user_script"],
				user_css = formdata["user_css"],
				user_unittests = formdata["user_unittests"],
				hostname = formdata["hostname"],
				dt = DateTime.Now
			});
			FILE.WriteAllText(newpath, json);
			
			return JsonData(new
			{
				code = newcode
			});
		}

		// POST: /APIFiddler/FiddlerNew
		[HttpPost]
		public ActionResult FiddlerUpdate()
		{
			AntiDoS();

			NameValueCollection formdata = Request.Form;

			string code = formdata["code"];
			string hostname = formdata["hostname"];

			if(!_code_regex.IsMatch(code))
				return new HttpNotFoundResult();

			string fiddlepath = Server.MapPath("~/App_Data/Fiddler/PrivateUserFiddles/" + code + ".json");
			if(!FILE.Exists(fiddlepath))
				return new HttpNotFoundResult();

			PrivateFiddle fiddle = JSON.DeserializeObject<PrivateFiddle>(FILE.ReadAllText(fiddlepath));
			if(fiddle.hostname != hostname)
				return new HttpNotFoundResult();

			var json = JSON.SerializeObject(new
			{
				title = formdata["title"],
				user_html = formdata["user_html"],
				user_script = formdata["user_script"],
				user_css = formdata["user_css"],
				user_unittests = formdata["user_unittests"],
				hostname = formdata["hostname"],
				dt = DateTime.Now
			});

			FILE.WriteAllText(fiddlepath, json);// overwrites
			return null;
		}

		// GET: /APIFiddler/FiddlerLoad
		public ActionResult FiddlerLoad(string code)
		{
			//AntiDoS();

			if(!_code_regex.IsMatch(code))
				return new HttpNotFoundResult();

			string path = Server.MapPath("~/App_Data/Fiddler/PrivateUserFiddles/" + code + ".json");
			if(!FILE.Exists(path))
				return new HttpNotFoundResult();
			string json = FILE.ReadAllText(path);

			PublicFiddle fiddle = JSON.DeserializeObject<PublicFiddle>(json);
			fiddle.code = code;
			return JsonData(fiddle);
		}


		// POST: /APIFiddler/CI_UploadTheLibrary
		[HttpPost]
		public string CI_UploadTheLibrary(string ver, string date)
		{
			try
			{
				string dirpath = Server.MapPath("~/CDN/Apps/TheLibrary/");
				if(DIR.Exists(dirpath))
					DIR.Delete(dirpath, true);
				DIR.CreateDirectory(dirpath);

				byte[] data = Utils.ReadFully(Request.InputStream);
				FILE.WriteAllBytes(dirpath + "TheLibraryWindows.zip", data);

				FILE.WriteAllText(dirpath + "ci_build.json", JSON.SerializeObject(new
				{
					ver = ver,
					date = date
				}));
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
			}
			return "DONE";
		}
	}
}
#endif
