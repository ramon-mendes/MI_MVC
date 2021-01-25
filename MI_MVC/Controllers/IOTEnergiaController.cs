using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.WebSockets;
using MI_MVC.DAL;
using MI_MVC.Models;
using Newtonsoft.Json.Linq;
using FILE = System.IO.File;
using JSON = Newtonsoft.Json.JsonConvert;

namespace MI_MVC.Controllers
{
	public class IOTEnergiaController : BaseController
    {
		private EnergiaContext db = new EnergiaContext();

		const double VALOR_KHW = 0.64298246;// $$
		const int TENSAO = 220;

		#region WebSocket
		private static int connectCount = 0;

		// GET: /IOTEnergia/WS
		public void WS()
		{
			if(HttpContext.IsWebSocketRequest)
			{
				connectCount++;
				HttpContext.AcceptWebSocketRequest(WebSocketRequestHandler, new AspNetWebSocketOptions() { RequireSameOrigin = false });
			}
			else if(HttpContext.IsWebSocketRequestUpgrading)
				LogMsg("MEOW");
			else
				LogMsg("FAIL");
		}

		public async Task WebSocketRequestHandler(AspNetWebSocketContext webSocketContext)
		{
			try
			{
				WebSocket webSocket = webSocketContext.WebSocket;

				/*We define a certain constant which will represent
				size of received data. It is established by us and 
				we can set any value. We know that in this case the size of the sent
				data is very small.
				*/
				const int maxMessageSize = 1024;

				//Buffer for received bits.
				var receivedDataBuffer = new ArraySegment<Byte>(new byte[maxMessageSize]);
				var cancellationToken = CancellationToken.None;

				while(webSocket.State == WebSocketState.Open)
				{
					WebSocketReceiveResult webSocketReceiveResult = await webSocket.ReceiveAsync(receivedDataBuffer, cancellationToken);

					//If input frame is cancelation frame, send close command.
					if(webSocketReceiveResult.MessageType == WebSocketMessageType.Close)
					{
						LogMsg("WebSocketMessageType.Close");
						await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, String.Empty, cancellationToken);
					}
					else
					{
						byte[] payloadData = receivedDataBuffer.Array.Where(b => b != 0).ToArray();
						string receiveString = Encoding.UTF8.GetString(payloadData, 0, payloadData.Length);
						string[] receiveSplitted = receiveString.Split('|');

						Push(double.Parse(receiveSplitted[0], CultureInfo.InvariantCulture),
								double.Parse(receiveSplitted[1], CultureInfo.InvariantCulture));

						var response = $"Server connects: {connectCount}";
						byte[] bytes = Encoding.UTF8.GetBytes(response);
						await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationToken);
					}
				}
				LogMsg("HERE1: " + webSocket.State.ToString());
			}
			catch(Exception ex)
			{
				LogMsg("HERE2: " + ex.ToString());
			}
		}
		#endregion



		// GET: /IOTEnergia
		public ActionResult Index()
		{
			return View();
		}

		// GET: /IOTEnergia/Logs
		public ActionResult Logs()
		{
			ViewBag._JSON_ = JSON.SerializeObject(db.Logs.ToList());
			return View();
		}

		// GET: /IOTEnergia/ClearLogs
		public ActionResult ClearLogs()
		{
			db.Logs.RemoveRange(db.Logs);
			db.SaveChanges();
			Alert("Registros apagados.");
			return RedirectToAction("Logs");
		}

		private void LogMsg(string msg)
		{
			db.Logs.Add(new EnergiaLog
			{
				dt = DateTime.Now,
				msg = msg
			});
			db.SaveChanges();
		}

		// GET: /IOTEnergia/Clear
		public ActionResult Clear()
		{
			db.AmpSamples.RemoveRange(db.AmpSamples);
			db.SaveChanges();
			Alert("Registros apagados.");
			return RedirectToAction("Dbg");
		}

		// GET: /IOTEnergia/Dbg
		public ActionResult Dbg()
		{
			ViewBag._JSON_ = JSON.SerializeObject(db.AmpSamples.ToList());
			return View();
		}

		private void CondenseHour()
		{

		}

		#region AJAX
		// GET: /IOTEnergia/AjaxHourGraph
		public ActionResult AjaxHourGraph()
		{
			var dt_from = db.AmpSamples.Take(1).Single().dt.AddHours(-1);
			var samples = db.AmpSamples.Where(s => s.dt >= dt_from)
				.Take(60)
				.ToList()
				.Select(s => new
				{
					dt = s.dt.ToUnixEpoch(),
					watt = Math.Round(s.amps * TENSAO, 2)
				})
				.ToList();
			Debug.Assert(samples.GroupBy(s => s.dt).Count() == 60);

			ViewBag._DATA_watts_lastmin = JSON.SerializeObject(samples);
			return null;
		}

		// GET: /IOTEnergia/AjaxWattsAndDay
		public ActionResult AjaxWattsAndDay()
		{
			var last = db.AmpSamples.OrderByDescending(s => s.SampleId).Take(1).Single();
			var dtfrom = DateTime.Now.Date;
			var day = db.ByHour.Where(hr => hr.dt > dtfrom).ToList();
			double day_khwsum = day.Sum(d => d.kwhsum);

			return JsonData(new
			{
				id = last.SampleId,
				dt = last.dt.ToString("dd/MM/yyyy HH:mm:ss"),
				watts = Math.Round(last.amps * 220, 2),
				voltage = TENSAO,
				amps = Math.Round(last.amps, 2),
				day_khwsum = day_khwsum,
				day_cost = day_khwsum * VALOR_KHW
			});
		}

		// GET: /IOTEnergia/AjaxClear
		public void AjaxClear()
		{
			db.AmpSamples.RemoveRange(db.AmpSamples);
			db.SaveChanges();
		}
		#endregion

		#region HW API
		// GET: /IOTEnergia/Push
		public string Push(double amps, double kwhsum)
		{
			Task.Run(() =>
			{
				db.AmpSamples.Add(new EnergiaSampleModel
				{
					dt = DateTime.Now,
					amps = amps,
					kwhsum = kwhsum
				});
				db.SaveChanges();
			});

			return "DONE";
		}
		#endregion
	}
}