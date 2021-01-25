using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MI_MVC.Models
{
	public class ChegueiVehicleModel
	{
		[Key]
		public int IdVehicle { get; set; }

		[Required]
		public string Name { get; set; }
	}
}