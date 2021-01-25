using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Web;
using System.Web.Mvc;
using MI_MVC.DAL;
using MI_MVC.Models;
using JSON = Newtonsoft.Json.JsonConvert;

namespace MI_MVC.Controllers
{
    public class IOTChegueiController : BaseController
	{
		private ChegueiContext db = new ChegueiContext();

		// GET: /IOTCheguei
		public ActionResult Index()
		{
			ViewBag.regs = db.Apoints.OrderByDescending(a => a.dt).ToList();
			return View();
		}

		// GET: /IOTCheguei/Clear
		public ActionResult Clear()
		{
			db.Apoints.RemoveRange(db.Apoints);
			db.SaveChanges();
			Alert("Registros apagados.");
			return RedirectToAction("Index");
		}

		#region HW API
		// GET: /IOTCheguei/Apoint
		public string Apoint(int IdReceiver, int IdVehicle, EApoint what, int rssi, int conn_rssi, int npackets, int inc, int dec, int highcnt, int highest)
		{
			db.Apoints.Add(new ChegueiApointModel()
			{
				what = what,
				dt = DateTime.Now,
				IdReceiver = IdReceiver,
				IdVehicle = IdVehicle,
				rssi = rssi,
				conn_rssi = conn_rssi,
				npackets = npackets,
				increases = inc,
				decreases = dec,
				highcnt = highcnt,
				highest = highest
			});
			db.SaveChanges();

			string result = "";
			using(var client = new WebClient())
			{
				client.Headers[HttpRequestHeader.ContentType] = "application/json";
				result = client.UploadString("https://maker.ifttt.com/trigger/iotcheguei/with/key/cWYx2qof4IbHTGG5PliY3", "POST", JSON.SerializeObject(new
				{
					value1 = what.ToString()
				}));
			}

			return "We all good!";
		}
		#endregion
	}
}