/*using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace IOTEnergiaCore.Models
{
	public class EnergiaContext : DbContext
	{
		public EnergiaContext(DbContextOptions<EnergiaContext> options)
			: base(options)
		{ }

		public DbSet<EnergiaSampleModel> Samples { get; set; }
		public DbSet<EnergiaByHourModel> ByHours { get; set; }
	}
}*/