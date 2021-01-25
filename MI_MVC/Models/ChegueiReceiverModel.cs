using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace MI_MVC.Models
{
	public class ChegueiReceiverModel
	{
		[Key]
		public int IdReceiver { get; set; }

		[Required]
		public string Name { get; set; }
		public string Endereco { get; set; }
	}
}