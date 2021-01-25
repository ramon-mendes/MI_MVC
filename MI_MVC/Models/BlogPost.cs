using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MI_MVC.Models
{
	public class BlogPost
    {
        public string Slug { get; set; }
        public string Title { get; set; }
        public string[] Tags { get; set; }
        public string Body { get; set; }
        public DateTime Date { get; set; }
        public string TimeAgo { get; set; }
    }
}