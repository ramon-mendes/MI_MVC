using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace MI_MVC.Models
{
	public class EnergiaSampleModel
	{
		[Key]
		public int SampleId { get; set; }
		public DateTime dt { get; set; }
		public double amps { get; set; }
		public double kwhsum { get; set; }
	}

	public class EnergiaByHourModel
	{
		[Key]
		public int Id { get; set; }
		public DateTime dt { get; set; }
		public double kwhsum { get; set; }
	}

	public class EnergiaLog
	{
		[Key]
		public int Id { get; set; }
		public DateTime dt { get; set; }
		public string msg { get; set; }
	}
}