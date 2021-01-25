using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MI_MVC.Models
{
	public enum EApoint
	{
		APPROACH,
		TIMEOUT_ARRIVE,
		TIMEOUT_LEAVE,
		TIMEOUT_NONE
	}

	public class ChegueiApointModel
	{
		public int Id { get; set; }
		public EApoint what { get; set; }
		public DateTime dt { get; set; }
		public int IdReceiver { get; set; }
		public int IdVehicle { get; set; }
		public int rssi { get; set; }
		public int conn_rssi { get; set; }
		public int npackets { get; set; }
		public int increases { get; set; }
		public int decreases { get; set; }
		public int highcnt { get; set; }
		public int highest { get; set; }

		public virtual ChegueiReceiverModel Receiver { get; set; }
		public virtual ChegueiVehicleModel Vehicle { get; set; }

		public HtmlString HTML_what()
		{
			switch(what)
			{
				case EApoint.APPROACH:
					return new HtmlString("<span class='badge badge-pill badge-info'>Passou</span>");
				case EApoint.TIMEOUT_ARRIVE:
					return new HtmlString("<span class='badge badge-pill badge-primary'>Chegou</span>");
				case EApoint.TIMEOUT_LEAVE:
					return new HtmlString("<span class='badge badge-pill badge-danger'>Saíu</span>");
				case EApoint.TIMEOUT_NONE:
					return new HtmlString("<span class='badge badge-pill badge-warning'>Saíu ou Entrou</span>");
			}
			return null;
		}
	}
}