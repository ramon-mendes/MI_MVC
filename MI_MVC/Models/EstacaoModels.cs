using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using MoreLinq;
using MI_MVC.DAL;
using System.Data.Entity;

namespace MI_MVC.Models
{
	public static class EstacaoData
	{
		public const string MODULE_ESTACAO = "ESTACAO";
		public const string MODULE_ESTUFA = "ESTUFA";
		public const string MODULE_SOIL = "SOIL";
		public const double PLUV_MM_TICK = 0.25;

		public class DaySummary
		{
			public DateTime dt;

			public double? temp1;
			public double? temp1Max;
			public double? temp1Min;
			public double? hum1;
			public double? hum1Max;
			public double? hum1Min;

			public double? temp2;
			public double? temp2Max;
			public double? temp2Min;
			public double? hum2;
			public double? hum2Max;
			public double? hum2Min;

			public double prec;
		}

		public static List<DaySummary> Days30Summary()
		{
			List<DaySummary> regs = new List<DaySummary>();
			using(var db = new EstacaoContext())
			{
				for(int i = 0; i < 30; i++)
				{
					var dt_day = DateTime.Now.AddDays(-i).Date;

					// temp/hum
					double? temp1Avg = null;
					double? temp1Min = null;
					double? temp1Max = null;
					double? hum1Avg = null;
					double? hum1Min = null;
					double? hum1Max = null;

					double? temp2Avg = null;
					double? temp2Min = null;
					double? temp2Max = null;
					double? hum2Avg = null;
					double? hum2Min = null;
					double? hum2Max = null;

					var data_ht_day1 = db.HTs.Where(d => d.module == MODULE_ESTACAO).Where(d => DbFunctions.TruncateTime(d.dt) == dt_day).ToList();
					if(data_ht_day1.Count != 0)
					{
						temp1Avg = Math.Round(data_ht_day1.Average(r => r.temp), 1);
						temp1Min = data_ht_day1.MinBy(r => r.temp).temp;
						temp1Max = data_ht_day1.MaxBy(r => r.temp).temp;
						hum1Avg = Math.Round(data_ht_day1.Average(r => r.hum), 1);
						hum1Min = data_ht_day1.MinBy(r => r.hum).hum;
						hum1Max = data_ht_day1.MaxBy(r => r.hum).hum;
					}

					var data_ht_day2 = db.HTs.Where(d => d.module == MODULE_ESTUFA).Where(d => DbFunctions.TruncateTime(d.dt) == dt_day).ToList();
					if(data_ht_day2.Count != 0)
					{
						temp2Avg = Math.Round(data_ht_day2.Average(r => r.temp), 1);
						temp2Min = data_ht_day2.MinBy(r => r.temp).temp;
						temp2Max = data_ht_day2.MaxBy(r => r.temp).temp;
						hum2Avg = Math.Round(data_ht_day2.Average(r => r.hum), 1);
						hum2Min = data_ht_day2.MinBy(r => r.hum).hum;
						hum2Max = data_ht_day2.MaxBy(r => r.hum).hum;
					}

					// pluv
					var data_pluv_day = db.PLUVs.OrderBy(r => r.dt).Where(d => DbFunctions.TruncateTime(d.dt) == dt_day).ToList();

					regs.Add(new DaySummary
					{
						dt = dt_day,
						hum1 = hum1Avg,
						hum1Min = hum1Min,
						hum1Max = hum1Max,
						temp1 = temp1Avg,
						temp1Min = temp1Min,
						temp1Max = temp1Max,
						hum2 = hum2Avg,
						hum2Min = hum2Min,
						hum2Max = hum2Max,
						temp2 = temp2Avg,
						temp2Min = temp2Min,
						temp2Max = temp2Max,
						prec = data_pluv_day.Count() * PLUV_MM_TICK
					});
				}
			}
			return regs.ToList();
		}
	}

	public class EstacaoHTModel
	{
		[Key]
		public int Id { get; set; }
		public string module { get; set; }
		public DateTime dt { get; set; }
		public double temp { get; set; }
		public double hum { get; set; }
	}

	public class EstacaoPLUVModel
	{
		[Key]
		public int Id { get; set; }
		public string module { get; set; }
		public DateTime dt { get; set; }
	}
	public class EstacaoPLUVByHourModel
	{
		[Key]
		public int Id { get; set; }
		public DateTime dt { get; set; }
		public double amount { get; set; }
	}

	public class EstacaoAnalogModel
	{
		[Key]
		public int Id { get; set; }
		public string module { get; set; }
		public DateTime dt { get; set; }
		public double v0 { get; set; }
		public double v1 { get; set; }
		public double v2 { get; set; }
		public double v3 { get; set; }
	}

	public class EstacaoModuleEvent
	{
		[Key]
		public int Id { get; set; }
		public string module { get; set; }
		public DateTime dt { get; set; }
		public string evt { get; set; }
	}
}