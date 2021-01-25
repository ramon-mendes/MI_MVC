using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Hosting;
using System.Text.RegularExpressions;

using DIR = System.IO.Directory;
using FILE = System.IO.File;
using JSON = Newtonsoft.Json.JsonConvert;

namespace MI_MVC.Controllers
{
	public class APIController : BaseController
	{
		#region DBG
		public static string received;

		// GET: /API/StoreData
		public string StoreData(string data)
		{
			received = data;
			return "Oh Yeahh!! Let me there";
		}

		// GET: /API/GetData
		public string GetData()
		{
			return received;
		}
		#endregion

		// GET: /API/Epoch
		public int Epoch()
		{
			return Utils.NowBrazil().ToUnixEpoch();
		}

		// POST: /API/CacheBlogPosts
		private static object _cacheLock = new object();
		private static DateTime _cacheLockOn;

		//[HttpPost] so I can GET in browser
		public string CacheBlogPosts()
		{
			// VEEEEEEEEEEEEEEERY COOL
			/*AlgoliaClient algolia = new AlgoliaClient("8YIKGY3IXP", "624b43907a23eeae99df283041e0bc77");
			var index = algolia.InitIndex("BlogPosts");
			index.ClearIndex();

			var posts = new BlogFileSystemManager().GetBlogListings();
			index.AddObjects(posts);*/


			if(!Monitor.TryEnter(_cacheLock))
				throw new Exception("CacheBlogPost already running, locked on " + _cacheLockOn);
			_cacheLockOn = DateTime.Now;

			try
			{
				Utils.SendTheMasterMail("", "CacheBlogPost STARTED");

				string zippath = Path.GetTempFileName();

				using(var client = new TimedWebClient())
				{
					ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
					client.DownloadFile("https://github.com/ramon-mendes/MI_BlogPosts/archive/master.zip", zippath);
				}

				string postsdir = Server.MapPath("~/App_Data/BlogPosts");
				string contentdir = Server.MapPath("~/ContentBlog");
				if(DIR.Exists(postsdir))
					DIR.Delete(postsdir, true);
				if(DIR.Exists(contentdir))
					DIR.Delete(contentdir, true);

				using(var zip = ZipFile.Open(zippath, ZipArchiveMode.Read))
				{
					zip.ExtractToDirectory(postsdir);
					DIR.Move(postsdir + "\\MI_BlogPosts-master", postsdir + "2");
					DIR.Delete(postsdir);
					DIR.Move(postsdir + "2", postsdir);
					DIR.Move(postsdir + "\\ContentBlog", contentdir);
				}

				Utils.SendTheMasterMail("", "CacheBlogPost DONE");
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
				throw;
			}
			Monitor.Exit(_cacheLock);

			var last_post = new BlogFileSystemManager().GetBlogListings().First();
			return "COOL, done. Last post: " + last_post.Title;
		}

		// GET: /API/UploadApp
		public string UploadApp(string dir, string filename)
		{
			string dirpath = Server.MapPath("~/CDN/Apps/" + dir + "/");
			if(!DIR.Exists(dirpath))
				DIR.CreateDirectory(dirpath);

			byte[] data = Utils.ReadFully(Request.InputStream);
			FILE.WriteAllBytes(dirpath + filename, data);

			return "DONE";
		}

		// GET: /API/ShowINPI
		public async Task<ActionResult> ShowINPI()
		{
			var baseAddress = new Uri("https://gru.inpi.gov.br/");
			var cookieContainer = new CookieContainer();
			using(var handler = new HttpClientHandler() { CookieContainer = cookieContainer, UseCookies = true })
			using(HttpClient hc = new HttpClient(handler) { BaseAddress = baseAddress })
			{
				var res1 = await hc.GetStringAsync("https://gru.inpi.gov.br/pePI/servlet/LoginController?action=login");

				var parameters = new Dictionary<string, string>
				{
					{ "NumPedido", "BR102017001025" },
					{ "NumGru", "" },
					{ "NumProtocolo", "" },
					{ "FormaPesquisa", "todasPalavras" },
					{ "ExpressaoPesquisa", "" },
					{ "Coluna", "Titulo" },
					{ "RegisterPerPage", "20" },
					{ "botao", "+pesquisar+%BB+" },
					{ "Action", "SearchBasico" },
				};
				var encodedContent = new FormUrlEncodedContent(parameters);

				var res2 = await hc.PostAsync("https://gru.inpi.gov.br//pePI/servlet/PatenteServletController", encodedContent);
				var contents = await res2.Content.ReadAsStringAsync();
				return Content(contents);
			}
		}


		#region SciterStatus
		[HttpPost]
		public void SciterStatus()
		{
			return;

			try
			{
				byte[] data = Utils.ReadFully(Request.InputStream);
				EncryptBlock(data);

				using(var mstream = new MemoryStream(data))
				{
					var zip = new ZipArchive(mstream);
					var entry_summary = zip.GetEntry("summary.txt");

					using(var sstream = new StreamReader(entry_summary.Open(), Encoding.UTF8))
					{
						string user_machine = sstream.ReadLine();
						string user_name = sstream.ReadLine();
						string user_name2 = sstream.ReadLine();
						string sent_seq = sstream.ReadLine();
						string assembly_name = sstream.ReadLine();
						string assembly_version = sstream.ReadLine();
						string assembly_entry = sstream.ReadLine();
						string what = sstream.ReadLine();

						if(user_machine.StartsWith("COREQUAD"))
							return;


						// Generate a GUID ID alphanumeric
						string id = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
						id = Regex.Replace(id, @"[^A-Za-z0-9]+", "");

						string zip_name = user_machine + '#' + user_name + '#' + id + ".zip";
						string zip_save_path = zip_name
							.Replace("\\", string.Empty)
							.Replace("/", string.Empty)
							.Replace("\0", string.Empty);
						zip_save_path = Server.MapPath("~/App_Data/SciterStatus/" + zip_save_path);

						using(var fzip = FILE.Create(zip_save_path))
						{
							if(data.Length == 0)
								throw new Exception("WHAT22?");

							fzip.Write(data, 0, data.Length);
						}

						// save screenshot
						Attachment att = null;
						if(zip.Entries.Any(e => e.Name == "screen.jpg"))
						{
							MemoryStream ms = new MemoryStream();
							zip.GetEntry("screen.jpg").Open().CopyTo(ms);
							ms.Seek(0, SeekOrigin.Begin);
							att = new Attachment(ms, "screen.jpg");
						}

						string body = $"Save .ZIP path: {zip_save_path}\n" +
							$"Size: {data.Length}\n" +
							$"Files: {string.Join(", ", zip.Entries.Select(e => e.Name))}\n" +
							$"Download: {Url.Action("SciterStatusDl", null, new { what = zip_name }, Request.Url.Scheme)}\n";
						string rest = sstream.ReadToEnd();
						body += rest;

						Utils.SendBootMail(body, "SciterStatus: " + user_machine + " - " + user_name + " - " + assembly_name + " - seq " + sent_seq + " - " + what, att);
					}
				}
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
			}
		}

		public ActionResult SciterStatusDl(string what)
		{
			return File(Server.MapPath("~/App_Data/SciterStatus/" + what), "application/zip");
		}

		private static void EncryptBlock(byte[] lpvBlock, string szPassword = "Régua de tomada WiFi")
		{
			int nPWLen = szPassword.Length;

			byte[] lpsPassBuff = Encoding.ASCII.GetBytes(szPassword);
			Debug.Assert(lpsPassBuff.Length == szPassword.Length);

			for(int nChar = 0, nCount = 0; nChar < lpvBlock.Length; nChar++)
			{
				byte cPW = lpsPassBuff[nCount];
				lpvBlock[nChar] ^= cPW;
				lpsPassBuff[nCount] = (byte)((cPW + 13) % 256);
				nCount = (nCount + 1) % nPWLen;
			}
		}

		public string ShowCode()
		{
			return Request.QueryString["code"];
		}
		#endregion

		#region Omni
		public static int OMNI_VERSION_MAJOR = Omni.Consts.VersionMajor;
		public static int OMNI_VERSION_MINOR = Omni.Consts.VersionMinor;

		private class EntryManifest
		{
			public string title;
			public string description;
			public bool native;
			public string main_asm;
		}

		private class EntryInfo
		{
			public EntryManifest manifest;
			public string name;
			public string url_page;
			public string url_github;
			public string url_preview;
			public string url_zipdl;
			public List<string> url_binaries;
		}

		// GET: /API/OmniRepos
		public ActionResult OmniRepos()
		{
			List<EntryInfo> result = new List<EntryInfo>();
			string cachedir = Server.MapPath("~/App_Data/Cache/PlaygroundCache/Playground-master");

			foreach(var dir in Directory.EnumerateDirectories(cachedir))
			{
				string preview = dir + "\\preview.png";
				string name = Path.GetFileName(dir);

				if(!FILE.Exists(dir + "\\manifest.json"))
					continue;
				if(!FILE.Exists(dir + "\\" + name + ".html"))
					continue;

				EntryManifest manifest = JSON.DeserializeObject<EntryManifest>(FILE.ReadAllText(dir + "\\manifest.json"));
				var info = new EntryInfo
				{
					manifest = manifest,
					name = name,
					url_page = "https://raw.githubusercontent.com/ramon-mendes/Playground/master/" + name + "/" + name + ".html",
					url_github = "https://github.com/ramon-mendes/Playground/tree/master/" + name,
					url_preview = FILE.Exists(preview) ? ("https://raw.githubusercontent.com/ramon-mendes/Playground/master/" + name + "/preview.png") : null,
					url_zipdl = "http://misoftware.com.br/API/Omni_PlaygroundDownloadZIP?name=" + name
				};

				if(manifest.native)
				{
					info.url_binaries = new List<string>();
					foreach(var path in Directory.EnumerateFiles(dir, "*.dll"))
					{
						string subpath = path.Substring(dir.Length).TrimStart('\\');
						info.url_binaries.Add("https://raw.githubusercontent.com/ramon-mendes/Playground/master/" + name + "/" + subpath);
					}
					foreach(var path in Directory.EnumerateFiles(dir, "*.exe"))
					{
						string subpath = path.Substring(dir.Length).TrimStart('\\');
						info.url_binaries.Add("https://raw.githubusercontent.com/ramon-mendes/Playground/master/" + name + "/" + subpath);
					}
				}

				result.Add(info);
			}

			return Content(JSON.SerializeObject(result), "application/json");
		}

		// GET: /API/Omni_PlaygroundDownloadZIP
		public ActionResult Omni_PlaygroundDownloadZIP(string name)
		{
			string cachedir = Server.MapPath("~/App_Data/Cache/PlaygroundCache/Playground-master");
			if(Directory.Exists(cachedir + "\\" + name))
			{
				string tmp = Path.GetTempFileName();
				if(FILE.Exists(tmp))
					FILE.Delete(tmp);
				ZipFile.CreateFromDirectory(cachedir + "\\" + name, tmp, CompressionLevel.Fastest, true);

				return File(tmp, "application/zip", name + ".zip");
			}
			return HttpNotFound();
		}

		// POST: /API/Omni_PlaygroundCache
		public ActionResult Omni_PlaygroundCache()
		{
			string zippath = Path.GetTempFileName();
			try
			{
				using(var client = new WebClient())
				{
					client.DownloadFile("https://github.com/ramon-mendes/Playground/archive/master.zip", zippath);
				}
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
			}

			string cachedir = Server.MapPath("~/App_Data/Cache/PlaygroundCache");
			if(Directory.Exists(cachedir))
				Directory.Delete(cachedir, true);

			ZipFile.ExtractToDirectory(zippath, cachedir);

			return new HttpStatusCodeResult(200, "COOL, relax");
		}
		#endregion

		#region OmniLite
		public static int OMNILITE_VERSION = 0x00010000;
		public static DateTime OMNILITE_RELEASE_DATE = new DateTime(2017, 4, 20);

		// GET: /API/OmniLiteInfo
		public ActionResult OmniLiteInfo()
		{
			const int XOR_CYPHER = 76542656;
			Thread.Sleep(1000);

			return JsonData(new
			{
				v = OMNILITE_VERSION ^ XOR_CYPHER,
				n = DateTime.UtcNow.ToUnixEpoch() ^ XOR_CYPHER,// NTP
				//r = OMNILITE_RELEASE_DATE.ToUnixEpoch() ^ XOR_CYPHER// last update date
			});
		}
		#endregion

		#region OmniCode
		public ActionResult OmniCodeInfo()
        {
            const int XOR_CYPHER = 407636546;

            return JsonData(new
            {
                v1 = 2 ^ XOR_CYPHER,// version major
                v2 = 0 ^ XOR_CYPHER,// version minor
                u = new DateTime(2016, 12, 1).AddDays(40).ToUnixEpoch() ^ XOR_CYPHER,// user MUST update after this date
                n = DateTime.UtcNow.ToUnixEpoch() ^ XOR_CYPHER// NTP
            });
        }
		#endregion
	}
}


/*public ActionResult OmniRepos()
{
	var cache_path = Server.MapPath("~/App_Data/Cache/OmniRepos-cache.json");
	if(FILE.Exists(cache_path) && (DateTime.Now - new FileInfo(cache_path).LastWriteTime).TotalDays < 3)
		return Content(FILE.ReadAllText(cache_path), "application/json");

	var client = new GitHubClient(new ProductHeaderValue("Omni"));
	var tokenAuth = new Credentials("eabbb9fb1abda88d9ff145ad5d00d6ccc49041f2");
	client.Credentials = tokenAuth;

	List<RepoInfo> result = new List<RepoInfo>();

	var dirs = client.Repository.Content.GetAllContents("MISoftware", "Omni-Playground").Result;
	foreach(var dir in dirs.Where(d => d.Type == ContentType.Dir))
	{
		string name = dir.Path;

		var files = client.Repository.Content.GetAllContents("MISoftware", "Omni-Playground", name).Result;

		// manifest.json
		if(!files.Any(f => f.Name == "manifest.json"))
			continue;
		var query_manifest = client.Repository.Content.GetAllContents("MISoftware", "Omni-Playground", name + "/manifest.json").Result;
		RepoManifest manifest = JSON.DeserializeObject<RepoManifest>(query_manifest.Single().Content);

		// find .html
		if(!files.Any(f => f.Name == name + ".html"))
			continue;

		var commit = client.Repository.Commit.GetAll("MISoftware", "Omni-Playground", new CommitRequest
		{
			Path = name
		}).Result.Last();

		var info = new RepoInfo
		{
			manifest = manifest,
			gitname = name,
			page_url = files.Single(f => f.Name == name + ".html").DownloadUrl.AbsoluteUri,
			updated_at = commit.Commit.Committer.Date.ToString("u")
		};

		// preview.png URL
		var preview = files.FirstOrDefault(f => f.Name == "preview.png");
		if(preview != null)
			info.preview = preview.DownloadUrl.AbsoluteUri;

		// github home URL
		info.github_url = dir.HtmlUrl.AbsoluteUri;

		// zipball URL
		info.zip_url = "http://misoftware.com.br/API/OmniReposDownloadZIP?name=" + name;

		result.Add(info);
	}

	string json = JSON.SerializeObject(result);
	FILE.WriteAllText(cache_path, json);
	return Content(json, "application/json");
}*/

// GET: /API/OmniNews
/*public ActionResult OmniNews()
{
	object[] news = new object[]
	{
		new
		{
			id = 2,
			date = "2015-09-09",
			title = "D language port of Sciter SDK",
			resume = "Checkout and download the D Language library which powers this app",
			htmlfile = "2015-09-09_sciter-dport.html"
		},

		new
		{
			id = 1,
			date = "2015-09-08",
			title = "Welcome to Omni",
			resume = "Sciter technology enables you, with a bit of HTML, CSS and scripting knowledge, to quickly create amazing desktop UI and Omni is the ideal tool for you developing needs.",
			htmlfile = "2015-09-08_OmniLaunch.html"
		},
	};

	return JsonData(news);
}*/