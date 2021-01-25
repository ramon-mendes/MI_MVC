using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using MI_MVC.Models;

namespace MI_MVC.DAL
{
	public class EstacaoContext : DbContext
	{
		public EstacaoContext()
			: base("EstacaoCompactDB")
		{
			Database.SetInitializer(new EstacaoInitializer());
		}

		public DbSet<EstacaoHTModel> HTs { get; set; }
		public DbSet<EstacaoPLUVModel> PLUVs { get; set; }
		//public DbSet<EstacaoPLUVByHourModel> PLUVsByHour { get; set; }
		public DbSet<EstacaoAnalogModel> Analogs { get; set; }
		public DbSet<EstacaoModuleEvent> Events { get; set; }
	}

	class EstacaoInitializer
		: DropCreateDatabaseIfModelChanges<EstacaoContext>
	{
		protected override void Seed(EstacaoContext db)
		{
#if !DEBUG
			return;
#endif
			var now = DateTime.Now;
			var now24 = now.AddHours(-24);
			var now_inc = now24;
			var rnd = new Random();

			db.PLUVs.Add(new EstacaoPLUVModel
			{
				module = EstacaoData.MODULE_ESTACAO,
				dt = now_inc,
			});

			while(now_inc < now)
			{
				if(rnd.NextDouble() < 0.3)
				{
					db.PLUVs.Add(new EstacaoPLUVModel
					{
						module = EstacaoData.MODULE_ESTACAO,
						dt = now_inc,
					});
				}
				
				db.HTs.Add(new EstacaoHTModel
				{
					module = EstacaoData.MODULE_ESTACAO,
					dt = now_inc,
					hum = rnd.NextDouble() * 90,
					temp = rnd.NextDouble() * 50
				});
				db.HTs.Add(new EstacaoHTModel
				{
					module = EstacaoData.MODULE_ESTUFA,
					dt = now_inc,
					hum = rnd.NextDouble() * 90,
					temp = rnd.NextDouble() * 50
				});
				db.Analogs.Add(new EstacaoAnalogModel
				{
					module = EstacaoData.MODULE_ESTACAO,
					dt = now_inc,
					v0 = rnd.NextDouble() * 3,
					v1 = rnd.NextDouble() * 3,
					v2 = rnd.NextDouble() * 3,
					v3 = rnd.NextDouble() * 3,
				});
				now_inc = now_inc.AddMinutes(5);
			}

			db.Analogs.Add(new EstacaoAnalogModel
			{
				module = EstacaoData.MODULE_SOIL,
				dt = DateTime.Now,
				v0 = 0.8,
				v1 = 3.3,
			});
		}
	}
}