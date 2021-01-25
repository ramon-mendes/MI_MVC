using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.WebSockets;
using System.ServiceModel.Syndication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.WebSockets;
using System.Xml;
using SimpleMvcSitemap;
using X.PagedList;
using JSON = Newtonsoft.Json.JsonConvert;

namespace MI_MVC.Controllers
{
	public class HomeController : BaseController
	{
		#region WS Test
		// GET: /Home/TestWS
		public ActionResult TestWS()
		{
			return View();
		}

		// GET: /Home/WSConnectCount
		private static int connectCount = 0;

		public void WSConnectCount()
		{
			connectCount++;

			if(HttpContext.IsWebSocketRequest || HttpContext.IsWebSocketRequestUpgrading)
			{
				Response.AppendHeader("Access-Control-Allow-Origin", "*");
				HttpContext.AcceptWebSocketRequest(WebSocketRequestHandler);
			}
			else
				throw new Exception("Not a WS connection");
		}

		public async Task WebSocketRequestHandler(AspNetWebSocketContext webSocketContext)
		{
			try
			{
				WebSocket webSocket = webSocketContext.WebSocket;

				const int maxMessageSize = 1024;

				//Buffer for received bits.
				var receivedDataBuffer = new ArraySegment<Byte>(new byte[maxMessageSize]);
				var cancellationToken = CancellationToken.None;

				while(webSocket.State == WebSocketState.Open)
				{
					//Reads data.
					WebSocketReceiveResult webSocketReceiveResult = await webSocket.ReceiveAsync(receivedDataBuffer, cancellationToken);

					//If input frame is cancelation frame, send close command.
					if(webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
					{
						await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, String.Empty, cancellationToken);
					}
					else
					{
						byte[] payloadData = receivedDataBuffer.Array.Where(b => b != 0).ToArray();
						string receiveString = Encoding.UTF8.GetString(payloadData, 0, payloadData.Length);

						var response = $"Hello! Connects count to this server with WebSocket: {connectCount}";
						byte[] bytes = Encoding.UTF8.GetBytes(response);
						await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
					}
				}
			}
			catch(Exception ex)
			{
				//Utils.SendBootMail(ex.ToString(), "what");
			}
		}
		#endregion

		private HttpCookie CreateMidiCookie()
		{
			HttpCookie MidiCookies = new HttpCookie("MidiView");
			MidiCookies.Value = "true";
			MidiCookies.Expires = DateTime.Now.AddHours(9999);
			return MidiCookies;
		}
		
		public string Dbg()
		{
			return Environment.UserDomainName;
			Utils.SendTheMasterMail("", "CacheBlogPost STARTED");
			Response.SetCookie(CreateMidiCookie());
			return Utils.IsUmbler.ToString();

			// https://github.com/bramstein/sfnt2woff-zopfli
			//return Process.Start(Server.MapPath("~\\App_Data\\ttf2woff.exe")).ToString();
			//return Process.Start("java").ToString();


			/*var dir = Server.MapPath("~/App_Data/SciterStatus");
			Directory.Delete(dir, true);
			Directory.CreateDirectory(dir);*/

			return Request.ServerVariables["SERVER_SOFTWARE"];
			//return Environment.MachineName;
		}

		public ActionResult Ajax()
		{
			return View();
		}

		public ActionResult Unittest()
		{
			return View();
		}

		public ActionResult Index(int? page)
		{
			var manager = new BlogFileSystemManager();
			var posts = manager.GetBlogListings();

			var blog_list = posts.ToPagedList(page ?? 1, 5);
			return View(blog_list);
		}

		public ActionResult Tag(string tag, int? page)
		{
			tag = Server.UrlDecode(tag);

			var manager = new BlogFileSystemManager();
			var posts = manager.GetTagListings(tag);

			var blog_list = posts.ToPagedList(page ?? 1, 5);
            ViewBag.Title  = "Tag '" + tag + "'";
			ViewBag.tag = tag;
			return View(blog_list);
		}

		public ActionResult Post(string slug)
		{
			var manager = new BlogFileSystemManager();
			var post = manager.GetPost(slug);

			if(post == null)
				return HttpNotFound();

            ViewBag.Title = post.Title;
			ViewBag.post_view = true;
			return View(post);
		}

		[HttpPost]
		public ActionResult Post(string slug, FormCollection form)
		{
			if(form["name"] == null)
			{
				Error("Please, write your 'Name'");
				return RedirectToAction("Post", new { slug = slug });
			}
			if(form["email"] == null)
			{
				Error("Please, write your 'E-mail'");
				return RedirectToAction("Post", new { slug = slug });
			}
			if(form["comment"] == null)
			{
				Error("Please, write your 'Comment'");
				return RedirectToAction("Post", new { slug = slug });
			}
			if(form["g-recaptcha-response"] == null)
			{
				Error("Please, confirm you are a human by answearing CAPTCHA.");
				return RedirectToAction("Post", new { slug = slug });
			}

			using(var client = new HttpClient())
			{
				//client.BaseAddress = new Uri("http://localhost:6740");
				var content = new FormUrlEncodedContent(new[]
				{
					new KeyValuePair<string, string>("secret", "6Lc6RgcUAAAAAAkyh9DTIA9_S0SyHloUpm4lbFZF"),
					new KeyValuePair<string, string>("response", form["g-recaptcha-response"]),
					new KeyValuePair<string, string>("remoteip", Request.UserHostAddress),
				});
				var result = client.PostAsync("https://www.google.com/recaptcha/api/siteverify", content).Result;
				string resultJSON = result.Content.ReadAsStringAsync().Result;

				var json = JSON.DeserializeObject<Dictionary<string, object>>(resultJSON);
				if((bool)json["success"] == false)
				{
					Error("Failed captcha verification.");
					return RedirectToAction("Post", new { slug = slug });
				}
			}

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("Name: " + form["name"]);
			sb.AppendLine("E-mail: " + form["email"]);
			sb.AppendLine("Comment: " + form["comment"]);
			sb.AppendLine("URL: " + "http://misoftware.com.br" + Url.Action("Post", new { slug = slug }));

			Utils.SendTheMasterMail(sb.ToString(), "MI Software - new comment");
			Alert("Comment submited with success! Wait for its approval.");

			return RedirectToAction("Post", new { slug = slug });
		}

		
		public ActionResult About()
		{
            ViewBag.Title  = "About";
			return View();
		}

		public ActionResult Services()
		{
            ViewBag.Title  = "Services";
			return View();
		}

        public void AjaxToggleFeatured()
		{
            if(Session["hide_featured"]==null)
                Session["hide_featured"] = true;
            else
                Session["hide_featured"] = !(bool) Session["hide_featured"];
		}

		public ActionResult RSS2()
		{
			var manager = new BlogFileSystemManager();
			var posts = manager.GetBlogListings();

			List<SyndicationItem> items = new List<SyndicationItem>();
			foreach(var item in posts)
			{
				items.Add(new SyndicationItem(
					item.Title,
					new CDataSyndicationContent(new TextSyndicationContent(item.Body, TextSyndicationContentKind.Html)),
					new Uri("http://misoftware.com.br/Home/Post/" + item.Slug),
					item.Slug,
					item.Date));
			}
			var dt_latupdate = posts.Max(p => p.Date);

			SyndicationFeed feed = new SyndicationFeed("MI Software", "Blog feed", new Uri("http://misoftware.com.br"), "FeedOneID", new DateTimeOffset(dt_latupdate));
            //feed.Authors.Add(new SyndicationPerson("someone@microsoft.com"));
            //feed.Categories.Add(new SyndicationCategory("How To Sample Code"));
            feed.Description = new TextSyndicationContent("RSS feed of MI Software's blog");
			feed.Items = items;

			var sw = new StringWriterWithEncoding();
			var xw = XmlWriter.Create(sw);
			new Atom10FeedFormatter(feed).WriteTo(xw);// Atom10FeedFormatter
			xw.Close();

			return File(sw.ToStream(), "application/rss+xml", "rss.xml");
		}

		public ActionResult Sitemap()
		{
			List<SitemapNode> nodes = new List<SitemapNode>
			{
				new SitemapNode(Url.Action("Index", "Home")),
				new SitemapNode(Url.Action("Misc", "Home")),
				new SitemapNode(Url.Action("About", "Home")),
				new SitemapNode(Url.Action("Services", "Home")),
				new SitemapNode(Url.Action("RSS2", "Home")),

                // Omni
				new SitemapNode(Url.Action("Index", "Omni")),
				new SitemapNode(Url.Action("Changelog", "Omni")),
				new SitemapNode(Url.Action("EULA", "Omni")),

                // OmniCode
				new SitemapNode(Url.Action("Index", "OmniCode")),
				new SitemapNode(Url.Action("Changelog", "OmniCode")),
				new SitemapNode(Url.Action("EULA", "OmniCode")),

                // Sciter Bootstrap
                new SitemapNode(Url.Action("Index", "Bootstrap")),
                new SitemapNode(Url.Action("Dev", "Bootstrap")),
                new SitemapNode(Url.Action("Templates", "Bootstrap")),
                new SitemapNode(Url.Action("Download", "Bootstrap")),

                // Sciter Playground
			};

			var posts = new BlogFileSystemManager().GetBlogListings();
			foreach(var postitem in posts)
			{
				nodes.Add(new SitemapNode(Url.Action("Post", "Home", new { slug = postitem.Slug })));
			}
			return new SitemapProvider().CreateSitemap(new SitemapModel(nodes));
		}
	}

	public sealed class StringWriterWithEncoding : StringWriter
	{
		private readonly Encoding encoding = Encoding.UTF8;

		public override Encoding Encoding
		{
			get { return encoding; }
		}

		public MemoryStream ToStream()
		{
			return new MemoryStream(encoding.GetBytes(ToString() ?? ""));
		}
	}

	public class CDataSyndicationContent : TextSyndicationContent
	{
		public CDataSyndicationContent(TextSyndicationContent content)
			: base(content)
		{ }

		protected override void WriteContentsTo(System.Xml.XmlWriter writer)
		{
			writer.WriteCData(Text);
		}
	}
}