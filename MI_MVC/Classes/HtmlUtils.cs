using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MI_MVC.Classes
{
	public static class HtmlUtils
	{
		public static object PrintIf(this HtmlHelper helper, bool cond, string html)
		{
			if( cond )
				return helper.Raw(html);
			return null;
		}
	}
}