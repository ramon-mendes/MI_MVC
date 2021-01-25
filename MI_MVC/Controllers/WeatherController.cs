using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using FILE = System.IO.File;
using JSON = Newtonsoft.Json.JsonConvert;

namespace MI_MVC.Controllers
{
    public class WeatherController : BaseController
    {
		const string OWM_APPID = "a34ccf247ac3a07cfbc8edf76dcefddf";
		const int CITY_ID = 3466537;// CXS

		public static void Setup()
		{
			new Thread(() =>
			{
				while(true)
				{
					DownloadStationData(4548, "StationPoa");
					DownloadStationData(4483, "StationCxs");
					Thread.Sleep(TimeSpan.FromMinutes(5));
				}
			}).Start();
		}

		private static void DownloadStationData(int id, string dir)
		{
			string dir_path = HostingEnvironment.MapPath($"~/App_Data/{dir}/");
			Directory.CreateDirectory(dir_path);

			try
			{
				string json = new WebClient().DownloadString($"http://api.openweathermap.org/data/2.5/station?id={id}&APPID=a34ccf247ac3a07cfbc8edf76dcefddf&units=metric");
				Debug.Assert(json != null);
				dynamic dynjson = JSON.DeserializeObject(json);

				// Essa station simplesmente desaparece do mapa, acho q por ficar away, e acaba retornando dynjson.cod==404
				if((object)dynjson.cod == null)
				{
					int epoch = dynjson.last.dt;
					string file_path = dir_path + epoch + ".json";
					if(!FILE.Exists(file_path))
						FILE.WriteAllText(file_path, json);
				}
			}
			catch(Exception)
			{
			}
		}

		// GET: /Weather
		public ActionResult Index()
        {
            return View();
        }

		// GET: /Weather/StationChart
		public ActionResult StationChart()
		{
			return View();
		}

		// GET: /Weather/StationChartImage
		public ActionResult StationChartImage()
		{
			var data = GetPoaStationData();

			PlotModel model = new PlotModel()
			{
				PlotAreaBorderColor = OxyColor.Parse("#BBBDBE"),
				TextColor = OxyColor.Parse("#50606F"),
			};

			var temperatureAxis = new LinearAxis()
			{
				Minimum = 0,
				Maximum = 400,
				MajorGridlineThickness = 1,
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineColor = OxyColor.Parse("#E9ECEF"),
				CropGridlines = true,
				TickStyle = TickStyle.None,
				Title = "Temperature Cº",
				Key = "Temperature",
			};
			model.Axes.Add(temperatureAxis);

			var humidityAxis = new LinearAxis()
			{
				Minimum = 0,
				Maximum = 100,
				Position = AxisPosition.Right,
				Title = "Humidity",
				Key = "Humidity",
			};
			model.Axes.Add(humidityAxis);

			var dateAxis = new DateTimeAxis()
			{
				StringFormat = "MMM dd",
				TickStyle = TickStyle.None
			};
			model.Axes.Add(dateAxis);

			LineSeries serie1 = new LineSeries()
			{
				MarkerType = MarkerType.Circle,
				MarkerSize = 4,
				MarkerStrokeThickness = 1,
				MarkerStroke = OxyColors.Black,
				MarkerFill = OxyColors.White,
				StrokeThickness = 3,
				ItemsSource = data.Select(e => new DataPoint(DateTimeAxis.ToDouble(e.dt), e.temperatureC)).ToList(),
				YAxisKey = "Temperature",
			};
			model.Series.Add(serie1);

			LineSeries serie2 = new LineSeries()
			{
				StrokeThickness = 1,
				ItemsSource = data.Select(e => new DataPoint(DateTimeAxis.ToDouble(e.dt), e.humidity)).ToList(),
				YAxisKey = "Humidity",
			};
			model.Series.Add(serie2);

			using (var ms = new MemoryStream())
			{
				var export = new PngExporter() { Width = 900 };
				export.Export(model, ms);
				return new ImageResult(ms.ToArray(), "image/png");
			}
		}

		// GET: /Weather/CxsForecastPlot
		public ActionResult CxsForecastPlot()
		{
			PlotModel model = new PlotModel()
			{
				PlotAreaBorderColor = OxyColor.Parse("#BBBDBE"),
				TextColor = OxyColor.Parse("#50606F"),
			};

			var temperatureAxis = new LinearAxis()
			{
				Minimum = 0,
				Maximum = 50,
				Title = "Temperature Cº",
				MajorGridlineThickness = 1,
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineColor = OxyColor.Parse("#E9ECEF"),
				CropGridlines = true,
				TickStyle = TickStyle.None
			};
			model.Axes.Add(temperatureAxis);

			var dateAxis = new DateTimeAxis()
			{
				StringFormat = "MMM dd",
				TickStyle = TickStyle.None
			};
			model.Axes.Add(dateAxis);

			LineSeries serie = new LineSeries()
			{
				MarkerType = MarkerType.Circle,
				MarkerSize = 4,
				MarkerStrokeThickness = 1,
				MarkerStroke = OxyColors.Black,
				MarkerFill = OxyColors.White,
				StrokeThickness = 3,
				ItemsSource = GetForecastData().Select(e => new DataPoint(DateTimeAxis.ToDouble(e.dt), e.temperatureC)).ToList()
			};
			model.Series.Add(serie);

			using(var ms = new MemoryStream())
			{
				var export = new PngExporter() { Width = 900 };
				export.Export(model, ms);
				return new ImageResult(ms.ToArray(), "image/png");
			}
		}

		// GET: /Weather/OmniGetWeatherData
		public ActionResult OmniGetWeatherData()
		{
			var data = GetPoaStationData();
			return JsonData(data);
		}

		class WeatherData
		{
			public DateTime dt;
			public double temperatureC;
			public double humidity;
		}

		private List<WeatherData> GetForecastData()
		{
			List<WeatherData> result = new List<WeatherData>();

			// GET the data from REST API
			// HttpClient need Microsoft.Net.Http NuGeT
			using(var client = new HttpClient())
			{
				HttpResponseMessage response = client.GetAsync($"http://api.openweathermap.org/data/2.5/forecast?id={CITY_ID}&APPID={OWM_APPID}&units=metric").Result;
				response.EnsureSuccessStatusCode();

				string json = Encoding.UTF8.GetString(response.Content.ReadAsByteArrayAsync().Result);
				dynamic dynjson = JSON.DeserializeObject(json);
				foreach(var item in dynjson.list)
				{
					result.Add(new WeatherData
					{
						dt = Utils.FromUnixTime((long) item.dt),
						temperatureC = (double) item.main.temp,
						humidity = (double) item.main.humidity
					});
				}
			}

			return result;
		}

		private List<WeatherData> GetPoaStationData()
		{
			List<WeatherData> result = new List<WeatherData>();

			string json_path = HostingEnvironment.MapPath("~/App_Data/StationPoa");
			var files = Directory.EnumerateFiles(json_path).ToList();
			Debug.Assert(files.CustomSort().SequenceEqual(files));

			Func<double, double> KtoC = (K) => K - 273.15;

			foreach(var item in files)
			{
				dynamic json = JSON.DeserializeObject(FILE.ReadAllText(item));
				if(json.last.main.temp==null || json.last.main.humidity==null)
					continue;
				
				result.Add(new WeatherData
				{
					dt = Utils.FromUnixTime((long)json.last.dt),
					temperatureC = KtoC((double)json.last.main.temp),
					humidity = (double)json.last.main.humidity
				});
			}

			return result;
		}
	}

	public class ImageResult : ActionResult
	{
		public ImageResult(byte[] image, string contentType)
		{
			if(image == null)
				throw new ArgumentNullException("image");
			if(contentType == null)
				throw new ArgumentNullException("contentType");

			this.Buffer = image;
			this.ContentType = contentType;
		}

		public byte[] Buffer { get; private set; }
		public string ContentType { get; private set; }

		public override void ExecuteResult(ControllerContext context)
		{
			if(context == null)
				throw new ArgumentNullException("context");

			HttpResponseBase response = context.HttpContext.Response;

			response.ContentType = this.ContentType;
			response.OutputStream.Write(Buffer, 0, Buffer.Length);
			response.End();
		}
	}
}