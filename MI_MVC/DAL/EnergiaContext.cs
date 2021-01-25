using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Linq;
using System.Web;
using MI_MVC.Models;

namespace MI_MVC.DAL
{
	public class EnergiaContext : DbContext
	{
		public EnergiaContext()
			: base("EnergiaCompactDB")
		{
			Database.SetInitializer(new EnergiaInitializer());
		}

		public DbSet<EnergiaSampleModel> AmpSamples { get; set; }
		public DbSet<EnergiaByHourModel> ByHour { get; set; }
		public DbSet<EnergiaLog> Logs { get; set; }
	}

	public class EnergiaInitializer
#if DEBUG
		: DropCreateDatabaseAlways<EnergiaContext>
#else
		: DropCreateDatabaseIfModelChanges<EnergiaContext>
#endif
	{
		protected override void Seed(EnergiaContext db)
		{
			return;

			Random rnd = new Random();

			// AmpSamples table
			{
				Debug.Assert(db.AmpSamples.Count()==0);

				var regs = new List<EnergiaSampleModel>();
				var now1hour = DateTime.Now.AddHours(-1);
				for(int j = 0; j < 60 * 60; j++)
				{
					var when = now1hour.AddSeconds(j);
					regs.Add(new EnergiaSampleModel
					{
						dt = when,
						amps = rnd.NextDouble() * 10
					});
				}
				db.AmpSamples.AddRange(regs);
			}

			// ByHour table
			{
				var now24 = DateTime.Now.AddHours(-24);
				for(int i = 0; i < 24; i++)
				{
					var when = now24.AddHours(i);
					db.ByHour.Add(new EnergiaByHourModel
					{
						dt = when,
						kwhsum = rnd.NextDouble() * 40
					});
				}
			}

			db.SaveChanges();
		}
	}
}