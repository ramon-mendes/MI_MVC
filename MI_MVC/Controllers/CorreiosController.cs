using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net;
using System.Diagnostics;
using System.Net.Http;
using HtmlAgilityPack;

namespace MI_MVC.Controllers
{
    public class CorreiosController : BaseController
    {
		class RegRow
		{
			public string data;
			public string local1;
			public string local2;
			public string situacao;
		}

		// GET: /Correios/PostCorreios
		public ActionResult PostCorreios(string code)
		{
			ViewBag.code = code;
			return View();
		}

		// GET: /Correios/GetObj
		public ActionResult GetObj(string code)
		{
			List<RegRow> regs = null;

			for(int i = 0; i < 10; i++)
			{
				try
				{
					regs = QueryRegs(code);
					break;
				}
				catch(Exception)
				{
					Thread.Sleep(1000);
				}
			}

			if(regs==null)
			{
				return JsonData(new { fail = true });
			}

			return JsonData(new
			{
				fail = false,
				arr_regs = regs
			});
		}

		private List<RegRow> QueryRegs(string code)
		{
			code = code.Trim();
			Debug.WriteLine("QueryRegs " + code);

			List<RegRow> regs = new List<RegRow>();
			using(HttpClient hc = new HttpClient())
			{
				var content = new FormUrlEncodedContent(new Dictionary<string, string>
				{
					{ "Objetos", code }
				});
				var response = hc.PostAsync("http://www2.correios.com.br/sistemas/rastreamento/resultado_semcontent.cfm", content).Result;
				response.EnsureSuccessStatusCode();
				var responseString = response.Content.ReadAsStringAsync().Result;


				HtmlDocument doc = new HtmlDocument();
				doc.LoadHtml(responseString);

				string xpath = "//table";
				var nodes = doc.DocumentNode.SelectNodes(xpath);
				if(nodes.Count != 1)
					throw new Exception();
				if(nodes == null)
					throw new Exception();

				Debug.Assert(nodes.Count == 1);
				var node = nodes.Single();

				var rows = node.SelectNodes("//tr").ToList();
				Debug.Assert(rows.Count >= 1);

				foreach(var cur_row in rows)
				{
					var td0 = cur_row.SelectNodes("td").First();
					var td1 = cur_row.SelectNodes("td").Last();

					var dt_arr = td0.InnerText.Split(new char[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).ToList();
					string dt = dt_arr[0] + " " + dt_arr[1];
					dt_arr.RemoveAt(0);
					dt_arr.RemoveAt(0);

					string sit = WebUtility.HtmlDecode(td1.SelectSingleNode("strong").InnerText);

					string loc1 = string.Join(" ", dt_arr);
					string loc2 = null;
					loc1 = WebUtility.HtmlDecode(loc1);

					var what = td1.ChildNodes.ToList();
					var nd_last = what.Last();
					if(nd_last.NodeType == HtmlNodeType.Text && nd_last.InnerText.Trim().Length != 0)
					{
						loc2 = WebUtility.HtmlDecode(nd_last.InnerText.Trim());
					}

					if(sit == "Objeto entregue ao destinatário")
						sit = "Entrega Efetuada";
					else if(sit == "Objeto saiu para entrega ao destinatário")
						sit = "Saiu para entrega ao destinatário";
					else if(sit == "Objeto encaminhado")
						sit = "Encaminhado";

					regs.Add(new RegRow()
					{
						data = dt,
						local1 = loc1,
						local2 = loc2,
						situacao = sit
					});
				}
			}

			return regs;
		}

		/* OLD METHOD
		public ActionResult GetObj(string code)
		{
			List<RegRow> regs = new List<RegRow>();

			using(var client = new HttpClient())
			{
				//string code = "JS687581435BR";
				//string code = "PN899501727BR";
				//string code = "PN854518651BR";

				var response = client.GetAsync("http://" + "websro.correios.com.br/sro_bin/txect01$.QueryList?P_LINGUA=001&P_TIPO=001&P_COD_UNI=" + code).Result;
				response.EnsureSuccessStatusCode();

				var responseString = response.Content.ReadAsStringAsync().Result;

				HtmlAgilityPack.HtmlDocument doc = new HtmlAgilityPack.HtmlDocument();
				doc.LoadHtml(responseString);

				string xpath = "//table";
				var nodes = doc.DocumentNode.SelectNodes(xpath);
				if(nodes == null)
				{
					return JsonData(new
					{
						fail = true
					});
				}

				Debug.Assert(nodes.Count == 1);
				var node = nodes.Single();

				var rows = node.SelectNodes("//tr");
				Debug.Assert(rows.Count >= 2);
				var rows2 = rows.Skip(1).ToList();

				// Iterate rows
				bool flag_colspan_row = false;

				foreach(var cur_row in rows2)
				{
					if(!flag_colspan_row)
					{
						var td0 = cur_row.ChildNodes[0];
						var td1 = cur_row.ChildNodes[1];
						var td2 = cur_row.ChildNodes[2];
						flag_colspan_row = td0.Attributes["rowspan"].Value == "2";

						regs.Add(new RegRow()
						{
							data = td0.InnerText,
							local1 = td1.InnerText,
							situacao = td2.InnerText
						});
					}
					else
					{
						var td0 = cur_row.ChildNodes.Single();
						var lastreg = regs.Last();
						lastreg.local2 = td0.InnerText;

						flag_colspan_row = false;
					}
				}
			}

			return JsonData(new
			{
				fail = false,
				arr_regs = regs
			});
		}*/
	}
}