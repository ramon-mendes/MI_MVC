using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;
using uPLibrary.Networking.M2Mqtt;
using System.Net;
using System.Diagnostics;
using OfficeOpenXml;
using MI_MVC.DAL;
using JSON = Newtonsoft.Json.JsonConvert;
using MI_MVC.Models;

namespace ConsoleTestes
{
	class Program
	{
		static void Main(string[] args)
		{
			//InstaPush();

			//Joiner.Setup();
			//*DAFAPI.Setup();

			MQTT_Push();
		}

		private static void MQTT_Push()
		{
			MqttClient client = new MqttClient("m12.cloudmqtt.com", 19948, false, null, null, MqttSslProtocols.None);

			string clientId = Guid.NewGuid().ToString();
			var res = client.Connect(clientId, "jqvuyevi", "EgIE0rsXSTdg");
			Debug.Assert(res == 0);
			client.Publish("what", Encoding.UTF8.GetBytes("WOW"));
		}

		private static void InstaPush()
		{
			var webRequest = (HttpWebRequest)WebRequest.Create("https://api.instapush.im/v1/post");
			if(webRequest != null)
			{
				var json = JSON.SerializeObject(new
				{
					title = "WOW",
					text = "MEOW"
				});
				var data = Encoding.ASCII.GetBytes(json);

				webRequest.Method = "POST";
				webRequest.ContentType = "application/json";
				webRequest.Headers.Add("X-INSTAPUSH-APPID", "5923420ca4c48aa125dbf15f");
				webRequest.Headers.Add("X-INSTAPUSH-APPSECRET", "87232e12f8908eb350eb6dbfd66fecca");
				webRequest.ContentLength = data.Length;

				using(var stream = webRequest.GetRequestStream())
				{
					stream.Write(data, 0, data.Length);
				}

				using(Stream s = webRequest.GetResponse().GetResponseStream())
				{
					using(StreamReader sr = new System.IO.StreamReader(s))
					{
						var jsonResponse = sr.ReadToEnd();
						Console.WriteLine(String.Format("Response: {0}", jsonResponse));
					}
				}
			}
		}
	}
}