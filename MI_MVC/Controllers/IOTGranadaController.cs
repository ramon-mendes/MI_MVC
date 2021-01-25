using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using uPLibrary.Networking.M2Mqtt;

namespace MI_MVC.Controllers
{
	public class IOTGranadaController : Controller
	{
		private static MqttClient _client;

		public static void Connect()
		{
			if(_client == null)
			{
				_client = new MqttClient("m13.cloudmqtt.com", 15567, false, null, null, MqttSslProtocols.None);
			}
			if(!_client.IsConnected)
			{
				string clientId = Guid.NewGuid().ToString();
				var res = _client.Connect(clientId, "lhaunjhz", "isXLlIfmg88F");
				Debug.Assert(res == 0);
			}
		}

		// GET: /IOTGranada
		public ActionResult Index()
		{
			Connect();
			return View();
		}

		// GET: /IOTGranada/Publish
		public void Publish(string topic, string msg)
		{
			Connect();
			var res = _client.Publish(topic, Encoding.UTF8.GetBytes(msg));
		}
	}
}