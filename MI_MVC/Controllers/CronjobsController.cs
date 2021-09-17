using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using HtmlAgilityPack;

using DIR = System.IO.Directory;
using FILE = System.IO.File;
using JSON = Newtonsoft.Json.JsonConvert;

namespace MI_MVC.Controllers
{
    public class CronjobsController : Controller
    {
		public static readonly string ROOT = HostingEnvironment.MapPath("~/");

		public static void Setup()
		{
			new Thread(() =>
			{
				//Thread.Sleep(TimeSpan.FromMinutes(2));

				while(true)
				{
					Utils.CatchAsMasterMail(() => GitDownloader());
					Utils.CatchAsMasterMail(() => BICE_SelfRenew());

					Thread.Sleep(TimeSpan.FromHours(4));
				}
			}).Start();

			new Thread(() =>
			{
#if !DEBUG
				Thread.Sleep(TimeSpan.FromMinutes(2));
#endif

				while(true)
				{
					RPI_Check().Wait();
					Thread.Sleep(TimeSpan.FromHours(1));
				}
			}).Start();
		}

		public static void GitDownloader()
		{
			using(HttpClient hc = new HttpClient())
			{
				var response = hc.GetAsync("https://github.com/ramon-mendes/Chamfer.js/archive/master.zip").Result;
				var data = response.Content.ReadAsByteArrayAsync().Result;
				var tmp = Path.GetTempFileName();

				FILE.WriteAllBytes(tmp, data);
				if(DIR.Exists(ROOT + "/Chamfer.js-master"))
					DIR.Delete(ROOT + "/Chamfer.js-master", true);

				using(MemoryStream ms = new MemoryStream(data))
				{
					ZipFile.ExtractToDirectory(tmp, ROOT);
				}
			}
		}

		#region RPI
		public static bool has_checked = false;

		public static async Task RPI_Check()// Task so I can await
		{
			if(DateTime.Now.DayOfWeek != DayOfWeek.Tuesday)
			{
				has_checked = false;
				return;
			}
			if(has_checked || DateTime.Now.Hour < 13)// de manhã o XML não foi gerado ainda, acho
			{
				return;
			}

			int weeks_passed = (int) (DateTime.Today - new DateTime(2017, 7, 18)).TotalDays / 7;
			int rpi_num = 2428 + weeks_passed;

			try
			{
				string txt_patentes;
				string txt_marcas = "";

				using(HttpClient hc = new HttpClient())
				{
					/*using(var zip = new ZipArchive(await hc.GetStreamAsync("http://revistas.inpi.gov.br/txt/RM" + rpi_num + ".zip")))
					{
						var entry = zip.GetEntry("RM" + rpi_num + ".xml");
						txt_marcas = new StreamReader(entry.Open()).ReadToEnd();
					}*/

					using(var zip = new ZipArchive(await hc.GetStreamAsync("http://revistas.inpi.gov.br/txt/P" + rpi_num + ".zip")))
					{
						var entry = zip.GetEntry("P" + rpi_num + ".txt");
						txt_patentes = new StreamReader(entry.Open()).ReadToEnd();
					}
				}

				// Escavador
				string[] termos = new[]
				{
					"BR 10 2017 001025", // patente granada
					"BR 10 2021 000372", // patente repetibox
					"Ramon Fernandes Mendes",
					"Walter Sengik da Cruz",
					"UBISTART",
					"BEYOUND",
					"DOMOTICS",
				};

				List<string> matchs_patentes = new List<string>();
				List<string> matchs_marcas = new List<string>();
				foreach(var termo in termos)
				{
					if(txt_patentes.IndexOf(termo, StringComparison.OrdinalIgnoreCase) != -1)
						matchs_patentes.Add(termo);
					if(txt_marcas.IndexOf(termo, StringComparison.OrdinalIgnoreCase) != -1)
						matchs_marcas.Add(termo);
				}

				if(matchs_patentes.Count != 0)
					Utils.SendTheMasterMail("MATCHS: " + string.Join(", ", matchs_patentes), "RPI Escavador " + rpi_num + " - PATENTES - MATHES!!!!!!!!!!!!!!!!!!");
				else
					Utils.SendTheMasterMail("NO MATCHS", "RPI Escavador " + rpi_num + " - PATENTES");

				if(matchs_marcas.Count != 0)
					Utils.SendTheMasterMail("MATCHS: " + string.Join(", ", matchs_marcas), "RPI Escavador " + rpi_num + " - MARCAS - MATHES!!!!!!!!!!!!!!!!!!");
				else
					Utils.SendTheMasterMail("NO MATCHS", "RPI Escavador " + rpi_num + " - MARCAS");

				has_checked = true;
			}
			catch(Exception ex)
			{
				Utils.SendMailLogException(ex);
			}
		}
		#endregion

		#region BICE
		private class BICE_User
		{
			public string name;
			public string login;
			public string pwd;
			public string email;

			public int renew_daycount;
			public DateTime renew_last_dt = DateTime.MinValue;
		}

		public class LoggingHandler : DelegatingHandler
		{
			public LoggingHandler(HttpMessageHandler innerHandler)
				: base(innerHandler)
			{
			}

			protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
			{
				Debug.WriteLine("Request:");
				Debug.WriteLine(request.ToString());
				if(request.Content != null)
				{
					Debug.WriteLine(await request.Content.ReadAsStringAsync());
				}
				Debug.WriteLine("");

				HttpResponseMessage response = await base.SendAsync(request, cancellationToken);

				Debug.WriteLine("Response:");
				Debug.WriteLine(response.ToString());
				if(response.Content != null)
				{
					Debug.WriteLine(await response.Content.ReadAsStringAsync());
				}
				Debug.WriteLine("");

				return response;
			}
		}
		
		public static void BICE_SelfRenew()
		{
			string htmlres = "LOG renovação:\n";

			List<BICE_User> users = new List<BICE_User>
			{
				new BICE_User { name = "Marlova", login = "36743", pwd = "04011", email = "marlova@advocacia.rs", renew_daycount = 13 },
				new BICE_User { name = "Ivo", login = "15094", pwd = "04011", email = "ivo@advocacia.rs", renew_daycount = 6 },
				new BICE_User { name = "Ramon", login = "79753", pwd = "88950", email = "ramon@misoftware.com.br", renew_daycount = 6 },
				new BICE_User { name = "Isadora", login = "380159", pwd = "17120", email = "isah.xp@hotmail.com", renew_daycount = 6 },
			};

			string json_path = HostingEnvironment.MapPath("~/App_Data/Cache/BICE_renew_dates.json");
			Utils.Ensure(() => json_path != null);
			List<BICE_User> renew_dates = JSON.DeserializeObject<List<BICE_User>>(FILE.ReadAllText(json_path));

			#region foreach user
			foreach(var user in users)
			{
				var renewed = renew_dates.SingleOrDefault(r => r.name == user.name);
				if(renewed != null)
					user.renew_last_dt = renewed.renew_last_dt;

				if((DateTime.Now - user.renew_last_dt).TotalDays < user.renew_daycount)
				{
					htmlres += "Usuário " + user.name + " - continue\n";
					continue;
				}

				try
				{
					var cookieContainer = new CookieContainer();

					HttpMessageHandler handler = new WebRequestHandler
					{
						UseCookies = true,
						CookieContainer = cookieContainer,
						AllowAutoRedirect = false
						//MaxAutomaticRedirections = 10
					};

					using(var client = new HttpClient(new LoggingHandler(handler)))
					{
						client.DefaultRequestHeaders.Referrer = new Uri("https://biblioteca.ucs.br/pergamum/biblioteca_s/php/login_usu.php");// indispensável
						client.DefaultRequestHeaders.Host = "biblioteca.ucs.br";
						client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");

						#region
						{
							var content = new FormUrlEncodedContent(new Dictionary<string, string>
							{
								{ "login", user.login },
								{ "password", user.pwd },
								{ "flag", "index.php" },
							});

							HttpResponseMessage response = client.PostAsync("https://biblioteca.ucs.br/pergamum/biblioteca_s/php/login_usu.php", content).Result;
							Utils.Ensure(() => response.StatusCode == HttpStatusCode.Redirect);
							Utils.Ensure(() => response.Headers.Location.OriginalString.StartsWith("sessao.php?nomepessoa2="));

							HttpResponseMessage response2 = client.GetAsync("https://biblioteca.ucs.br/pergamum/biblioteca_s/php/" + response.Headers.Location).Result;
							Utils.Ensure(() => response2.StatusCode == HttpStatusCode.Redirect);
							Utils.Ensure(() => response2.Headers.Location.OriginalString == "../meu_pergamum/index.php?flag=index.php");

							HttpResponseMessage response3 = client.GetAsync("https://biblioteca.ucs.br/pergamum/biblioteca_s/meu_pergamum/index.php?flag=").Result;
							response3.EnsureSuccessStatusCode();
						}
						#endregion

						// Data Extraction - booklist ---------------------------------------------------------------------------------------------------------
						int nbooks = 0;
						List<string> data_renewids = new List<string>();
						#region
						{
							var response = client.GetAsync("https://biblioteca.ucs.br/pergamum/biblioteca_s/meu_pergamum/emp_renovacao.php").Result;
							response.EnsureSuccessStatusCode();

							var responseString = response.Content.ReadAsStringAsync().Result;
							HtmlDocument doc = new HtmlDocument();
							doc.LoadHtml(responseString);

							string xpath = "//div[@id='meio']//td[@class='borda_iframe']/table";
							var nodes = doc.DocumentNode.SelectNodes(xpath);
							if(nodes != null)// usuário sem livros!
							{
								Utils.Ensure(() => nodes.Count > 2);

								nbooks = nodes.Count - 2;

								for(int i = 1; i < nodes.Count - 1; i++)
								{
									var node = nodes[i];
									node = node.SelectSingleNode(".//input[@type='checkbox']");
									data_renewids.Add(node.Attributes["value"].Value);
								}

								Utils.Ensure(() => data_renewids.Count == nbooks);
							}
						}
						#endregion

						// Renew POST request -------------------------------------------------------------------------------------------------
						Dictionary<string, string> error_by_code = new Dictionary<string, string>();
						#region
						if(nbooks != 0)
						{
							string renewids = string.Join(";", data_renewids) + ';';
							var content = new FormUrlEncodedContent(new Dictionary<string, string>
							{
								{ "renova", "renovar" },
								{ "Selecs", renewids },
							});

							HttpResponseMessage response = client.PostAsync("https://biblioteca.ucs.br/pergamum/biblioteca_s/meu_pergamum/emp_renovacao.php", content).Result;
							response.EnsureSuccessStatusCode();

							var responseString = response.Content.ReadAsStringAsync().Result;
							HtmlDocument doc = new HtmlDocument();
							doc.LoadHtml(responseString);

							// Data Extraction - renew result
							var dom_res_bookid = doc.DocumentNode.SelectSingleNode("//input[@name='exemplares']");
							var dom_res_errors = doc.DocumentNode.SelectSingleNode("//input[@name='erros']");

							string val_bookid = Encoding.GetEncoding("ISO-8859-1").GetString(Convert.FromBase64String(dom_res_bookid.Attributes["value"].Value));
							string val_errors = Encoding.GetEncoding("ISO-8859-1").GetString(Convert.FromBase64String(dom_res_errors.Attributes["value"].Value));
							var data_result_booksid = val_bookid.Split(';');
							var data_result_errors = val_errors.Split(new char[] { ';' });

							for(int i = 0; i < nbooks; i++)
							{
								string cod = data_result_booksid[i].Split('@')[0];
								error_by_code[cod] = data_result_errors[i];
							}
						}
						#endregion

						// Request - Updated books list HTML page -------------------------------------------------------------------------------------------------
						#region
						if(nbooks != 0)
						{
							string responseString = client.GetStringAsync("https://biblioteca.ucs.br/pergamum/biblioteca_s/meu_pergamum/emp_renovacao.php").Result;
							HtmlDocument doc = new HtmlDocument();
							doc.LoadHtml(responseString);

							string xpath = "//div[@id='meio']//td[@class='borda_iframe']/table";
							var nodes = doc.DocumentNode.SelectNodes(xpath);
							Utils.Ensure(() => nodes.Count - 2 == nbooks);

							StringBuilder sb = new StringBuilder();
							sb.AppendLine("Renovação de livros automática - " + user.name);
							sb.AppendLine("----------------------------------------------------------------------");

							for(int i = 1; i < nodes.Count - 1; i++)
							{
								var node_line = nodes[i];

								string cod = node_line.SelectSingleNode(".//td[2]/text()").InnerText;
								string title = node_line.SelectSingleNode(".//td[3]/text()").InnerText;
								string untildate = node_line.SelectSingleNode(".//td[4]/text()").InnerText;

								sb.AppendLine(string.Format(
									"\n" +
									"Livro: {0} - {1}\n" +
									"Nova data devolução: {2}\n" +
									"{3}\n",
									cod,
									title,
									untildate,
									error_by_code[cod].Length == 0 ? "-> RENOVADO COM SUCESSO" : ("-> ERRO AO RENOVAR!! : " + error_by_code[cod])
								));
							}

							Utils.SendMailTo(user.email, "Renovação automática BICE", sb.ToString());
							Utils.SendMailTo("ramon@misoftware.com.br", "Renovação automática BICE", sb.ToString());

							htmlres += sb.ToString();
						}
						#endregion

						user.renew_last_dt = DateTime.Now;
					}
				}
				catch(Exception ex)
				{
					Utils.SendMailLogException(ex);
				}
			}
			#endregion

			FILE.WriteAllText(json_path, JSON.SerializeObject(users, Newtonsoft.Json.Formatting.Indented));
			//return Content(htmlres, "text/plain");
		}
		#endregion
	}
}