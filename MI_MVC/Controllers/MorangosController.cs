using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;
using System.Web.Hosting;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.WindowsForms;
using uPLibrary.Networking.M2Mqtt;
using MoreLinq;
using MI_MVC.DAL;
using MI_MVC.Models;

using JSON = Newtonsoft.Json.JsonConvert;
using FILE = System.IO.File;
using OfficeOpenXml;
using System.Globalization;
using System.Data.Entity;

namespace MI_MVC.Controllers
{
    public class MorangosController : BaseController
	{
		private static MqttClient _client;
		private EstacaoContext db = new EstacaoContext();

		private static void Connect()
		{
			if(_client == null)
			{
				MqttClient client = new MqttClient("m13.cloudmqtt.com", 15567, false, null, null, MqttSslProtocols.None);

				string clientId = Guid.NewGuid().ToString();
				var res = client.Connect(clientId, "lhaunjhz", "isXLlIfmg88F");
				Debug.Assert(res==0);

				_client = client;
			}
		}

		#region TABELAS
		// GET: /Morangos/Tabelas
		public ActionResult Tabelas()
		{
			return View();
		}

		// GET: /Morangos/GetTabela
		public ActionResult GetTabela(string name)
		{
			switch(name)
			{
				case "EstacaoHTModel":
					return JsonData(db.HTs.ToList());
				case "EstacaoPLUVModel":
					return JsonData(db.PLUVs.ToList());
				case "EstacaoAnalogModel":
					return JsonData(db.Analogs.ToList());
				case "EstacaoModuleEvent":
					return JsonData(db.Events.ToList());
			}
			return View();
		}

		// GET: /Morangos/ClearTabela
		public void ClearTabela(string name)
		{
			switch(name)
			{
				case "EstacaoHTModel":
					db.HTs.RemoveRange(db.HTs.ToList());
					break;
				case "EstacaoPLUVModel":
					db.PLUVs.RemoveRange(db.PLUVs.ToList());
					break;
				case "EstacaoAnalogModel":
					db.Analogs.RemoveRange(db.Analogs.ToList());
					break;
				case "EstacaoModuleEvent":
					db.Events.RemoveRange(db.Events.ToList());
					break;
			}
			db.SaveChanges();
		}
		#endregion

		// GET: /Morangos/Dbg
		public string Dbg()
		{
			Connect();
			return "OK";
		}

		// GET: /Morangos/Log
		public ActionResult Log()
		{
			ViewBag._JSON_ = JSON.SerializeObject(db.Events.ToList());
			return View();
		}

		// GET: /Morangos/LogClear
		public ActionResult LogClear()
		{
			db.Events.RemoveRange(db.Events);
			db.SaveChanges();
			Alert("Registros apagados.");
			return RedirectToAction("Log");
		}

		// GET: /Morangos/Relatorio
		public ActionResult Relatorio()
		{
			using(EstacaoContext db = new EstacaoContext())
			{
				using(ExcelPackage package = new ExcelPackage())
				{
					//Add the Content sheet
					var ws = package.Workbook.Worksheets.Add("Relatório");

					//Headers
					ws.Cells["A1"].Value = "Data";
					ws.Cells["B1"].Value = "Temp. média";
					ws.Cells["C1"].Value = "Temp. min";
					ws.Cells["D1"].Value = "Temp. max";
					ws.Cells["E1"].Value = "Hum. média";
					ws.Cells["F1"].Value = "Hum. min";
					ws.Cells["G1"].Value = "Hum. max";
					ws.Cells["H1"].Value = "Precipitação (mm)";
					ws.Cells["A1:H1"].Style.Font.Bold = true;

					int irow = 2;
					foreach(var item in EstacaoData.Days30Summary())
					{
						ws.Cells[irow, 1].Value = item.dt.ToString("dd/MM/yyyy");
						ws.Cells[irow, 2].Value = item.temp1;
						ws.Cells[irow, 3].Value = item.temp1Min;
						ws.Cells[irow, 4].Value = item.temp1Max;
						ws.Cells[irow, 5].Value = item.hum1;
						ws.Cells[irow, 6].Value = item.hum1Min;
						ws.Cells[irow, 7].Value = item.hum1Max;
						ws.Cells[irow, 8].Value = item.prec;
						irow++;
					}

					//package.Save();
					return File(package.GetAsByteArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "relatorio.xlsx");
				}
			}
		}

		// GET: /Morangos
		public ActionResult Index()
		{
			//Connect();
#if false
			db.Analogs.Add(new EstacaoAnalogModel
			{
				dt = DateTime.Now,
				v0 = 1,
				v1 = 1,
				v2 = 1,
				v3 = 1,
			});
			db.SaveChanges();
#endif

			var data_ht = GetHTData24(EstacaoData.MODULE_ESTACAO).Select(r => new
			{
				dt = r.dt.ToUnixEpoch(),
				hum = Math.Round(r.hum, 2),
				temp = Math.Round(r.temp, 2),
			}).ToList();

			if(data_ht.Count==0)
				throw new Exception("Inicio do dia, sem dados ainda coletados, aguarde 5 minutos.");

			ViewBag._DATA_HT_qtd = data_ht.Count;
			ViewBag._DATA_HT_expected = (72 * 60 * 60) / HT_PUSH_EVERY.TotalSeconds;
			ViewBag._DATA_summary = JSON.SerializeObject(SummaryDayData());
			return View();
		}

		// GET: /Morangos/GraficosDia
		public ActionResult GraficosDia(string date)
		{
			if(date == null)
			{
				Error("Informe a data");
				return RedirectToAction("Index");
			}

			DateTime dt = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
			ViewBag._DATA_summary = JSON.SerializeObject(SummaryDateData(dt));
			ViewBag.date = date;
			return View();
		}

		public object SummaryDayData()
		{
			var dt_when = Utils.NowBrazil().Date;
			var data_ht24estacao = db.HTs.Where(d => d.module == EstacaoData.MODULE_ESTACAO).Where(d => d.dt >= dt_when).OrderBy(r => r.dt).ToList();
			var data_ht24estufa = db.HTs.Where(d => d.module == EstacaoData.MODULE_ESTUFA).Where(d => d.dt >= dt_when).OrderBy(r => r.dt).ToList();
			var data_pluv24 = db.PLUVs.OrderBy(r => r.dt).Where(d => d.dt >= dt_when).ToList();
			var data_analogs24 = db.Analogs.OrderBy(r => r.dt).Where(d => d.dt >= dt_when).ToList();

			Utils.Ensure(() => data_ht24estacao.Any());
			Utils.Ensure(() => data_ht24estufa.Any());
			Utils.Ensure(() => data_analogs24.Any());

			var temp1Min = data_ht24estacao.MinBy(r => r.temp);
			var temp1Max = data_ht24estacao.MaxBy(r => r.temp);
			var hum1Min = data_ht24estacao.MinBy(r => r.hum);
			var hum1Max = data_ht24estacao.MaxBy(r => r.hum);

			var temp2Min = data_ht24estufa.MinBy(r => r.temp);
			var temp2Max = data_ht24estufa.MaxBy(r => r.temp);
			var hum2Min = data_ht24estufa.MinBy(r => r.hum);
			var hum2Max = data_ht24estufa.MaxBy(r => r.hum);

			var v0Min = data_analogs24.MinBy(r => r.v0);
			var v1Min = data_analogs24.MinBy(r => r.v1);
			var v2Min = data_analogs24.MinBy(r => r.v2);
			var v3Min = data_analogs24.MinBy(r => r.v3);
			var v0Max = data_analogs24.MaxBy(r => r.v0);
			var v1Max = data_analogs24.MaxBy(r => r.v1);
			var v2Max = data_analogs24.MaxBy(r => r.v2);
			var v3Max = data_analogs24.MaxBy(r => r.v3);

			// dew point
			double Temperature = data_ht24estacao.Last().temp;
			double RelativeHumidity = data_ht24estacao.Last().hum;
			double VapourPressureValue = RelativeHumidity * 0.01 * 6.112 * Math.Exp((17.62 * Temperature) / (Temperature + 243.12));
			double Numerator = 243.12 * Math.Log(VapourPressureValue) - 440.1;
			double Denominator = 19.43 - (Math.Log(VapourPressureValue));
			double DewPoint = Math.Round(Numerator / Denominator, 1);

			Func<double, string> f_volts = (v) => Math.Round(v, 2) + "V";
			return new
			{
				precipitacao = (data_pluv24.Count * EstacaoData.PLUV_MM_TICK).ToString() + " mm",

				temp1 = data_ht24estacao.Last().temp + " C°",
				temp1Min = temp1Min.temp + " C° às " + temp1Min.dt.ToString("HH:mm"),
				temp1Max = temp1Max.temp + " C° às " + temp1Max.dt.ToString("HH:mm"),
				hum1 = data_ht24estacao.Last().hum + "%",
				hum1Min = hum1Min.hum + "% às " + hum1Min.dt.ToString("HH:mm"),
				hum1Max = hum1Max.hum + "% às " + hum1Max.dt.ToString("HH:mm"),
				dewPoint = "(P.O: " + DewPoint + " C°)",

				temp2 = data_ht24estufa.Last().temp + " C°",
				temp2Min = temp2Min.temp + " C° às " + temp2Min.dt.ToString("HH:mm"),
				temp2Max = temp2Max.temp + " C° às " + temp2Max.dt.ToString("HH:mm"),
				hum2 = data_ht24estufa.Last().hum + "%",
				hum2Min = hum2Min.hum + "% às " + hum2Min.dt.ToString("HH:mm"),
				hum2Max = hum2Max.hum + "% às " + hum2Max.dt.ToString("HH:mm"),

				v0 = f_volts(data_analogs24.Last().v0),
				v0Min = f_volts(v0Min.v0) + " às " + v0Min.dt.ToString("HH:mm"),
				v0Max = f_volts(v0Max.v0) + " às " + v0Max.dt.ToString("HH:mm"),
				v1 = f_volts(data_analogs24.Last().v1),
				v1Min = f_volts(v0Min.v1) + " às " + v1Min.dt.ToString("HH:mm"),
				v1Max = f_volts(v0Max.v1) + " às " + v1Max.dt.ToString("HH:mm"),
				v2 = f_volts(data_analogs24.Last().v2),
				v2Min = f_volts(v0Min.v2) + " às " + v2Min.dt.ToString("HH:mm"),
				v2Max = f_volts(v0Max.v2) + " às " + v2Max.dt.ToString("HH:mm"),
				v3 = f_volts(data_analogs24.Last().v3),
				v3Min = f_volts(v0Min.v3) + " às " + v3Min.dt.ToString("HH:mm"),
				v3Max = f_volts(v0Max.v3) + " às " + v3Max.dt.ToString("HH:mm"),

				module1_state = (Utils.NowBrazil() - data_ht24estacao.Last().dt).TotalMinutes <= 10,
				module1_lastread = data_ht24estacao.Last().dt.ToString("dd/MM/yyyy HH:mm"),

				module2_state = (Utils.NowBrazil() - data_ht24estufa.Last().dt).TotalMinutes <= 10,
				module2_lastread = data_ht24estufa.Last().dt.ToString("dd/MM/yyyy HH:mm"),
			};
		}

		public object SummaryDateData(DateTime when)
		{
			var dt_when = Utils.NowBrazil().Date;
			var data_ht24 = db.HTs.OrderBy(r => r.dt).Where(d => d.dt >= dt_when).ToList();
			var data_pluv24 = db.PLUVs.OrderBy(r => r.dt).Where(d => d.dt >= dt_when).ToList();
			var data_analogs24 = db.Analogs.OrderBy(r => r.dt).Where(d => d.dt >= dt_when).ToList();
			Utils.Ensure(() => data_ht24.Any());
			Utils.Ensure(() => data_analogs24.Any());

			var tempAvg = data_ht24.Average(r => r.temp);
			var tempMin = data_ht24.MinBy(r => r.temp);
			var tempMax = data_ht24.MaxBy(r => r.temp);
			var humAvg = data_ht24.Average(r => r.hum);
			var humMin = data_ht24.MinBy(r => r.hum);
			var humMax = data_ht24.MaxBy(r => r.hum);

			return new
			{
				precipitacao = (data_pluv24.Count * EstacaoData.PLUV_MM_TICK).ToString() + " mm",
				tempAvg = Math.Round(tempAvg) + " C°",
				tempMin = tempMin.temp + " C° às " + tempMin.dt.ToString("HH:mm"),
				tempMax = tempMax.temp + " C° às " + tempMax.dt.ToString("HH:mm"),
				humAvg = Math.Round(humAvg) + "%",
				humMin = humMin.hum + "% às " + humMin.dt.ToString("HH:mm"),
				humMax = humMax.hum + "% às " + humMax.dt.ToString("HH:mm")
			};
		}

		#region REGUA
		// GET: /Morangos/PublishQOS
		public void PublishQOS(string topic, string msg)
		{
			Connect();
			// returns an integer, the message number
			var nmsg = _client.Publish(topic, Encoding.UTF8.GetBytes(msg), 2, false);
		}

		// POST: /Morangos/SubmitReguaSchedule
		[HttpPost]
		public ActionResult SubmitReguaSchedule()
		{
			var form = Request.Form;
			var dic = form.AllKeys.ToDictionary(k => k, k => form[k]);
			if(dic["ligado"] == "ON")
			{
				var arr_dates = dic["dt"].Split(',');
				var arr_duration = dic["dur"].Split(',');
				var json_data = arr_dates.Select((s, i) => new
				{
					start_hour = int.Parse(arr_dates[i].Split(':')[0]),
					start_min = int.Parse(arr_dates[i].Split(':')[1]),
					duration = int.Parse(arr_duration[i])
				}).ToArray();
				PublishQOS("morangos/regua/scheduleON", JSON.SerializeObject(json_data));
			}
			else
			{
				PublishQOS("morangos/regua/scheduleOFF", "");
			}

			Alert("Configurações enviadas");
			return RedirectToAction("Index");
		}

		// POST: /Morangos/SubmitReguaManual
		[HttpPost]
		public ActionResult SubmitReguaManual()
		{
			var form = Request.Form;
			var dic = form.AllKeys.ToDictionary(k => k, k => form[k]);
			var json = JSON.SerializeObject(dic);
			PublishQOS("morangos/regua/manual", json);

			Alert("Configurações enviadas");
			return RedirectToAction("Index");
		}
		#endregion

		#region AJAX
		// GET: /Morangos/AjaxGetAnalog
		public ActionResult AjaxGetAnalog()
		{
			var data_estacao = db.Analogs.Where(a => a.module == EstacaoData.MODULE_ESTACAO).OrderByDescending(a => a.Id).First();

			return JsonData(new
			{
				dt_estacao = data_estacao.dt.ToString("dd/MM/yyyy HH:mm:ss"),
				v0 = Math.Round(data_estacao.v0, 3),
				v1 = Math.Round(data_estacao.v1, 3),
				v2 = Math.Round(data_estacao.v2, 3),
				v3 = Math.Round(data_estacao.v3, 3),

				/*dt_soil = data_soil.dt.ToString("dd/MM/yyyy HH:mm:ss"),
				v0soil = vsoil,
				v1soil = vin,
				p1 = p1,
				p2 = p2,
				p3 = p3,*/
			});
		}

		public (EstacaoAnalogModel, double) GetSoilWithKPA()
		{
			var data_soil = db.Analogs.Where(a => a.module == EstacaoData.MODULE_SOIL).OrderByDescending(a => a.Id).First();

			double vsoil = data_soil.v0;
			double vin = data_soil.v1;
			// http://www.kimberly.uidaho.edu/water/swm/Calibration_Watermark2.htm
			double KNOWN_RESISTOR = 2.130;// 2k1

			double r = KNOWN_RESISTOR * (vin - vsoil) / vsoil;
			var temp = 24;
			double p1 = -20 * (r * (1 + 0.018 * (temp - 24)) - 0.55);
			double p2 = (-3.213 * r - 4.093) / (1 - 009733 * r - 0.01205 * temp);
			double p3part = 1 + 0.018 * (temp - 24);
			double p3 = -2.246 - 5.239 * r * (1 + 0.018 * (temp - 24)) - 0.06756 * r * r * (p3part * p3part);
			return (data_soil, vsoil);
		}
		#endregion

		#region DATA
		private readonly TimeSpan HT_PUSH_EVERY = TimeSpan.FromMinutes(5);

		private List<EstacaoHTModel> GetHTDataByDate(DateTime dt_day, string module)
		{
			var a = db.HTs.ToList();
			var b = a.Last();
			bool c = b.dt.Date == dt_day;
			return db.HTs.Where(r => r.module==module).OrderBy(r => r.dt).ToList().Where(d =>
			{
				return d.dt.Date == dt_day;
			}).ToList();
		}
		private List<EstacaoHTModel> GetHTData24(string module)
		{
			var data = db.HTs.ToList();
			var dt_from = Utils.NowBrazil().AddHours(-24);
			return data.Where(r => r.module == module).Where(r => r.dt > dt_from).OrderBy(r => r.dt).ToList();
		}

		private List<EstacaoPLUVByHourModel> GetPLUVDataByDate(DateTime dt_day, string module)
		{
			var pluv_data = db.PLUVs.Where(r => r.module == module).Where(d => DbFunctions.TruncateTime(d.dt) == dt_day).ToList();

			var res = new List<EstacaoPLUVByHourModel>();
			for(int i = 0; i < 24; i++)
			{
				var when = dt_day.AddHours(i);
				var what = pluv_data.Where(r => r.dt.Date == when.Date && r.dt.Hour == when.Hour);
				res.Add(new EstacaoPLUVByHourModel()
				{
					dt = when,
					amount = what.Count() * EstacaoData.PLUV_MM_TICK
				});
			}
			return res;
		}

		private List<EstacaoPLUVByHourModel> GetPLUVDataByHour48(string module)
		{
			var now = Utils.NowBrazil();
			now = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0);

			var pluv_data = db.PLUVs.ToList();
			var res = new List<EstacaoPLUVByHourModel>();
			for(int i = 0; i < 48; i++)
			{
				var when = now.AddHours(-i);
				var what = pluv_data.Where(r => r.dt.Date == when.Date && r.dt.Hour == when.Hour);
				res.Add(new EstacaoPLUVByHourModel()
				{
					dt = when,
					amount = what.Count() * EstacaoData.PLUV_MM_TICK
				});
			}
			return res;
		}
		#endregion

		#region GRÁFICOS
		// GET: /Morangos/ImgChartPLUV
		public ActionResult ImgChartPLUV(string date)
		{
			List<EstacaoPLUVByHourModel> data;
			if(date != null)
				data = GetPLUVDataByDate(DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture), EstacaoData.MODULE_ESTACAO);
			else
				data = GetPLUVDataByHour48(EstacaoData.MODULE_ESTACAO);

			PlotModel _model = new PlotModel()
			{
				PlotAreaBorderColor = OxyColor.Parse("#BBBDBE"),
				DefaultFontSize = 11,
			};

			var pluvAxis = new LinearAxis()
			{
				AbsoluteMinimum = 0,
				MinimumRange = 8,
				AxislineStyle = LineStyle.Solid,
				TickStyle = TickStyle.Outside,
				StringFormat = "#,0",
				MajorGridlineColor = OxyColor.Parse("#CDD2D6"),
				MajorGridlineStyle = LineStyle.Solid
			};

			var categoryAxis = new CategoryAxis()
			{
				Position = AxisPosition.Bottom,
				Angle = 45,
			};
			categoryAxis.Labels.AddRange(data.Select(e => e.dt.ToString("HH'h'")).Reverse());

			ColumnSeries serie = new ColumnSeries()
			{
				Title = "Precipitação mm/h",
				ColumnWidth = 20,
				LabelFormatString = "{0:.00}",
				FillColor = OxyColor.FromAColor(180, OxyColor.Parse("#A3C7D8")),
				StrokeThickness = 1.5,
				StrokeColor = OxyColor.Parse("#8AB9CF"),
				ItemsSource = data.Select(e => new ColumnItem(e.amount)).Reverse(),
			};

			_model.Series.Add(serie);
			_model.Axes.Add(categoryAxis);
			_model.Axes.Add(pluvAxis);

			/*PlotModel model = new PlotModel()
			{
				PlotAreaBorderColor = OxyColor.Parse("#BBBDBE"),
				TextColor = OxyColor.Parse("#50606F"),
				IsLegendVisible = true,
				LegendPlacement = LegendPlacement.Inside,
				LegendSymbolLength = 50
			};

			var pluvAxis = new LinearAxis()
			{
				AxislineStyle = LineStyle.Solid,
				TickStyle = TickStyle.Outside,
				StringFormat = "#,0"
			};

			var categoryAxis = new CategoryAxis()
			{
				TickStyle = TickStyle.None,
				MaximumRange = 1
			};
			categoryAxis.Labels.AddRange(data.Select(e => e.dt.ToString("dd/MMM HH:mm")));

			ColumnSeries serie = new ColumnSeries()
			{
				Title = "Precipitação",
				StrokeThickness = 1.5,
				ColumnWidth = 20,
				LabelPlacement = LabelPlacement.Inside,
				LabelFormatString = "{0:.00}% wtf",
				ItemsSource = data.Select(e => new ColumnItem(e.amount))
			};

			model.Series.Add(serie);
			model.Axes.Add(categoryAxis);
			model.Axes.Add(pluvAxis);*/

			using(var ms = new MemoryStream())
			{
				var export = new PngExporter() { Width = 1100, Height = 400 };
				export.Export(_model, ms);
				return new ImageResult(ms.ToArray(), "image/png");
			}
		}

		// GET: /Morangos/ImgChartHT
		public ActionResult ImgChartHT(string date)
		{
			List<EstacaoHTModel> data1;
			if(date != null)
				data1 = GetHTDataByDate(DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture), EstacaoData.MODULE_ESTACAO);
			else
				data1 = GetHTData24(EstacaoData.MODULE_ESTACAO);

			List<EstacaoHTModel> data2;
			if(date != null)
				data2 = GetHTDataByDate(DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture), EstacaoData.MODULE_ESTUFA);
			else
				data2 = GetHTData24(EstacaoData.MODULE_ESTUFA);

			PlotModel model = new PlotModel()
			{
				PlotAreaBorderColor = OxyColor.Parse("#BBBDBE"),
				TextColor = OxyColor.Parse("#50606F"),
				IsLegendVisible = true,
				LegendPlacement = LegendPlacement.Inside,
				LegendSymbolLength = 50
			};

			double min = Math.Min(0, data1.Min(d => d.temp));
			if(min < 0)
				min--;

			model.Axes.Add(new LinearAxis()
			{
				Minimum = min,
				Maximum = 40,
				MajorGridlineThickness = 1,
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineColor = OxyColor.Parse("#E9ECEF"),
				CropGridlines = true,
				TickStyle = TickStyle.None,
				Key = "Temperatura",
				TicklineColor = OxyColor.Parse("#FF0000"),
			});

			model.Axes.Add(new LinearAxis()
			{
				Minimum = min,
				Maximum = 100,
				Position = AxisPosition.Right,
				Key = "Humidade",
			});

			model.Axes.Add(new DateTimeAxis()
			{
				StringFormat = "dd/MMM HH:mm",
				TickStyle = TickStyle.None,
				MaximumRange = 1
			});

			model.Series.Add(new LineSeries()
			{
				Title = "Temperatura Estação Cº",
				YAxisKey = "Temperatura",
				StrokeThickness = 2.5,
				ItemsSource = data1.Select(e => new DataPoint(DateTimeAxis.ToDouble(e.dt), e.temp)).ToList(),
				Color = OxyColor.Parse("#0000FF"),
				LineStyle = LineStyle.Solid,
				InterpolationAlgorithm = new CanonicalSpline(1)
			});

			model.Series.Add(new LineSeries()
			{
				Title = "Humidade Estação",
				YAxisKey = "Humidade",
				StrokeThickness = 2.5,
				ItemsSource = data1.Select(e => new DataPoint(DateTimeAxis.ToDouble(e.dt), e.hum)).ToList(),
				LineStyle = LineStyle.LongDashDot,
				InterpolationAlgorithm = new CanonicalSpline(1)
			});

			model.Series.Add(new LineSeries()
			{
				Title = "Temperatura Estufa Cº",
				YAxisKey = "Temperatura",
				StrokeThickness = 1,
				ItemsSource = data2.Select(e => new DataPoint(DateTimeAxis.ToDouble(e.dt), e.temp)).ToList(),
				Color = OxyColor.Parse("#CD0000"),
				LineStyle = LineStyle.LongDashDotDot,
				InterpolationAlgorithm = new CanonicalSpline(1)
			});

			model.Series.Add(new LineSeries()
			{
				Title = "Humidade Estufa",
				YAxisKey = "Humidade",
				StrokeThickness = 1,
				ItemsSource = data2.Select(e => new DataPoint(DateTimeAxis.ToDouble(e.dt), e.hum)).ToList(),
				Color = OxyColor.Parse("#005098"),
				LineStyle = LineStyle.Dash,
				InterpolationAlgorithm = new CanonicalSpline(1)
			});

			using(var ms = new MemoryStream())
			{
				var export = new PngExporter() { Width = 1100, Height = 400 };
				export.Export(model, ms);
				return new ImageResult(ms.ToArray(), "image/png");
			}
		}

		// GET: /Morangos/ImgChartAnalog
		public ActionResult ImgChartAnalog(string date)
		{
			List<EstacaoAnalogModel> data;
			if(date != null)
			{
				DateTime dt_when = DateTime.ParseExact(date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
				data = db.Analogs.Where(r => DbFunctions.TruncateTime(r.dt) == dt_when).OrderBy(r => r.dt).ToList();
			}
			else
			{
				var dt_from = Utils.NowBrazil().AddHours(-24);
				data = db.Analogs.Where(r => r.dt > dt_from).ToList();
			}

			double MAX_VOLTAGE = 3.3;

			PlotModel model = new PlotModel()
			{
				PlotAreaBorderColor = OxyColor.Parse("#BBBDBE"),
				TextColor = OxyColor.Parse("#50606F"),
			};

			// Y axes
			model.Axes.Add(new LinearAxis()
			{
				Minimum = 0,
				Maximum = MAX_VOLTAGE,
				MajorGridlineThickness = 1,
				MajorGridlineStyle = LineStyle.Solid,
				MajorGridlineColor = OxyColor.Parse("#E9ECEF"),
				CropGridlines = true,
				Key = "v0",
				TicklineColor = OxyColor.Parse("#FF0000"),
				TickStyle = TickStyle.Inside,
			});
			model.Axes.Add(new LinearAxis()
			{
				Minimum = 0,
				Maximum = MAX_VOLTAGE,
				Key = "v1",
				TickStyle = TickStyle.None,
			});
			model.Axes.Add(new LinearAxis()
			{
				Minimum = 0,
				Maximum = MAX_VOLTAGE,
				Key = "v2",
				TickStyle = TickStyle.None,
			});
			// X axis
			model.Axes.Add(new DateTimeAxis()
			{
				StringFormat = "MMM dd HH:mm",
				TickStyle = TickStyle.None,
				MaximumRange = 1
			});

			model.Series.Add(new LineSeries()
			{
				Title = "Molhamento folhar 1",
				StrokeThickness = 1,
				ItemsSource = data.Select(e => new DataPoint(DateTimeAxis.ToDouble(e.dt), e.v0)).ToList(),
				YAxisKey = "v0",
				Color = OxyColor.Parse("#0000FF"),
				//InterpolationAlgorithm = new CanonicalSpline(1),
			});

			model.Series.Add(new LineSeries()
			{
				Title = "Molhamento folhar 2",
				StrokeThickness = 1,
				ItemsSource = data.Select(e => new DataPoint(DateTimeAxis.ToDouble(e.dt), e.v1)).ToList(),
				YAxisKey = "v1",
				Color = OxyColor.Parse("#FF0000"),
				LineStyle = LineStyle.Dot,
				//InterpolationAlgorithm = new CanonicalSpline(1)
			});

			model.Series.Add(new LineSeries()
			{
				Title = "Molhamento folhar 3",
				StrokeThickness = 1,
				ItemsSource = data.Select(e => new DataPoint(DateTimeAxis.ToDouble(e.dt), e.v2)).ToList(),
				YAxisKey = "v2",
				LineStyle = LineStyle.Dash,
				//InterpolationAlgorithm = new CanonicalSpline(1)
			});

			using(var ms = new MemoryStream())
			{
				var export = new PngExporter() { Width = 1100, Height = 500 };
				export.Export(model, ms);
				return new ImageResult(ms.ToArray(), "image/png");
			}
		}
		#endregion

		#region HW API
		// GET: /Morangos/NTP
		public int NTP()
		{
			return Utils.NowBrazil().ToUnixEpoch();
		}

		// GET: /Morangos/PushHT
		public void PushHT(string module, double hum, double temp)
		{
			db.HTs.Add(new EstacaoHTModel
			{
				dt = Utils.NowBrazil(),
				module = module,
				hum = hum,
				temp = temp
			});
			db.SaveChanges();
		}

		// GET: /Morangos/PushPluvTick
		public void PushPluvTick(string module)
		{
			db.PLUVs.Add(new EstacaoPLUVModel
			{
				module = module,
				dt = Utils.NowBrazil()
			});
			db.SaveChanges();
		}

		// GET: /Morangos/PushADC
		public void PushADC(string module, double v0, double v1, double v2, double v3)
		{
			db.Analogs.Add(new EstacaoAnalogModel()
			{
				module = module,
				dt = Utils.NowBrazil(),
				v0 = v0,
				v1 = v1,
				v2 = v2,
				v3 = v3,
			});
			db.SaveChanges();
		}


		// GET: /Morangos/PushSoil
		public void PushSoil(string module, double v0)
		{
			db.Analogs.Add(new EstacaoAnalogModel()
			{
				module = module,
				dt = Utils.NowBrazil(),
				v0 = v0,
			});
			db.SaveChanges();
		}

		// GET: /Morangos/PushEvent
		public void PushEvent(string module, string evt)
		{
			db.Events.Add(new EstacaoModuleEvent
			{
				module = module,
				dt = Utils.NowBrazil(),
				evt = evt,
			});
			db.SaveChanges();
		}
		#endregion
	}
}