using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Octokit;
using System.Threading.Tasks;

namespace MI_MVC.Controllers
{
	public class PgItem
	{
		public string gitauthor;
		public string gitname;
		public string updated_at;
		public dynamic manifest;
		public string page_url;
		public string preview;
		public string github_url;
		public string zip_url;
	}

	public class PlaygroundController : BaseController
	{
		// GET: Playground
		public ActionResult Index()
		{
			string filepath;
#if DEBUG
			filepath = @"C:\wamp\www\sciter_api\repos.json";
#else
			filepath = Server.MapPath(@"cdn\sciter_api\repos.json");
#endif
			string json = System.IO.File.ReadAllText(filepath);
			var picks = JsonConvert.DeserializeObject<List<PgItem>>(json);
			return View(picks);
		}

		// POST: Playground/SubmitPage
		[HttpPost]
		public async Task<ActionResult> SubmitPage(string repo_url)
		{
			// check URL
			Regex rg = new Regex(@"^https?://github.com/([\w\d\.]+)/([\w\d\.]+)$");
			var match = rg.Match(repo_url);
			if(!match.Success)
			{
				Error("Invalid repository URL");
				return RedirectToAction("Index");
			}


			var client = new GitHubClient(new ProductHeaderValue("playground"));
			var basicAuth = new Credentials("eabbb9fb1abda88d9ff145ad5d00d6ccc49041f2"); // NOTE: not real credentials
			client.Credentials = basicAuth;

			var user = await client.User.Current();
			var a = user.Name;

			return RedirectToAction("Index");
		}
	}
}