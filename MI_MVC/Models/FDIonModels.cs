using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MI_MVC.Models
{
	public class FDIonLicenseModel
	{
		public int Id { get; set; }
		public string serial { get; set; }
		public bool activated { get; set; }
		public DateTime dt_activate { get; set; }
	}

	public class FDIonActivationModel
	{
		[Key]
		public int Id { get; set; }

		public string name { get; set; }
		public string email { get; set; }

		public string HID { get; set; }
		public string activation_key { get; set; }
		public DateTime dt_expires { get; set; }
		public bool OS { get; set; }// true = windows
	}

	public class FDLog
	{
		[Key]
		public int Id { get; set; }
		public DateTime dt { get; set; }
		public string log { get; set; }
	}

	public class FDFeedback
	{
		[Key]
		public int Id { get; set; }
		public DateTime dt { get; set; }
		public string email { get; set; }
		public string thoughts { get; set; }
	}
}