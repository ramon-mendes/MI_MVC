using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using X.PagedList.Mvc;
using X.PagedList.Mvc.Common;

namespace MI_MVC
{
	public static class PaginationStyle
	{
		static PaginationStyle()
		{
			MIPaging = new PagedListRenderOptions
			{
				DisplayLinkToFirstPage = PagedListDisplayMode.Never,
				DisplayLinkToLastPage = PagedListDisplayMode.Never,
				DisplayLinkToPreviousPage = PagedListDisplayMode.Always,
				DisplayLinkToNextPage = PagedListDisplayMode.Always,
				DisplayLinkToIndividualPages = false,
				LiElementClasses = new[] { "page-item" },
				PageClasses = new[] { "page-link" },
			};
		}

		public static PagedListRenderOptions MIPaging = new PagedListRenderOptions
		{
			DisplayLinkToFirstPage = PagedListDisplayMode.Never,
			DisplayLinkToLastPage = PagedListDisplayMode.Never,
			DisplayLinkToPreviousPage = PagedListDisplayMode.Always,
			DisplayLinkToNextPage = PagedListDisplayMode.Always,
			DisplayLinkToIndividualPages = false,
			LiElementClasses = new[] { "page-item" },
			PageClasses = new[] { "page-link" },
		};
	}
}