using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using MI_MVC.Models;

namespace MI_MVC.DAL
{
	public class ChegueiContext : DbContext
	{
		public ChegueiContext()
			: base("ChegueiCompactDB")
		{
			Database.SetInitializer<ChegueiContext>(new ChegueiInitializer());
		}

		public DbSet<ChegueiApointModel> Apoints { get; set; }
		public DbSet<ChegueiVehicleModel> Vehicles { get; set; }
		public DbSet<ChegueiReceiverModel> Receivers { get; set; }
	}

	public class ChegueiInitializer
		: DropCreateDatabaseIfModelChanges<ChegueiContext>
	{
		protected override void Seed(ChegueiContext db)
		{
			db.Vehicles.Add(new ChegueiVehicleModel
			{
				Name = "March"
			});
			db.Vehicles.Add(new ChegueiVehicleModel
			{
				Name = "Corsa Gian"
			});

			db.Receivers.Add(new ChegueiReceiverModel
			{
				Name = "Casa Ramon"
			});
			db.Receivers.Add(new ChegueiReceiverModel
			{
				Name = "Casa Leonel"
			});
			db.SaveChanges();
		}
	}
}