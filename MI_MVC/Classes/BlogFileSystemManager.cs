using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using MI_MVC.Models;

namespace MI_MVC
{
	public class BlogFileSystemManager
	{
		private readonly string PATH_BLOG_POSTS;// = System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data/BlogPosts/");
		//private HeyRed.MarkdownSharp.Markdown _mkd;

		public BlogFileSystemManager()
		{
			if(Utils.IsLocalHost)
				PATH_BLOG_POSTS = "D:\\MVC\\MI_BlogPosts";
			else
				PATH_BLOG_POSTS = HttpContext.Current.Server.MapPath("~/App_Data/BlogPosts/");

			//_mkd = new HeyRed.MarkdownSharp.Markdown();
			//_mkd.AddExtension(new MkdCode());
		}

		public List<BlogPost> GetBlogListings()
		{
			var allFileNames = Directory.GetFiles(PATH_BLOG_POSTS, "*.json", SearchOption.AllDirectories)
				.Where(path => !path.Contains("\\.vscode\\"))
				.Where(path => !path.Contains("\\Pending\\"))
				.Where(path => !path.Contains("\\ContentBlog\\"))
				.CustomSortByFilename()
				.Reverse()
				.ToList();

			var blogListings = new List<BlogPost>();
			foreach(var filePath in allFileNames)//.Take(limit)
			{
				var summaryData = File.ReadAllText(filePath);
				var postBody = File.ReadAllText(filePath.Substring(0, filePath.Length-5) + ".md");
				string fileName = Path.GetFileNameWithoutExtension(filePath);

				var blogPost = JsonConvert.DeserializeObject<BlogPost>(summaryData);
				blogPost.Slug = fileName.Substring(11, fileName.Length - 11);
				blogPost.Body = TransformMarkdown(postBody);
				blogPost.Date = DateTime.Parse(fileName.Substring(0, 10));
				blogPost.TimeAgo = ToRelativeDate(blogPost.Date);

				if(!Utils.IsLocalHost)
				{
					if(blogPost.Date > DateTime.Now)
						continue;
				}
				
				blogListings.Add(blogPost);
			}

			return blogListings;
		}

		public List<BlogPost> GetTagListings(string tag)
		{
			var allFileNames = Directory.GetFiles(PATH_BLOG_POSTS, "*.json", SearchOption.AllDirectories)
				.CustomSortByFilename()
				.Reverse()
				.Where(path => !path.Contains("\\Pending\\"))
				.Where(path => !path.Contains("\\ContentBlog\\"))
				.ToList();

			var blogListings = new List<BlogPost>();
			foreach(var filePath in allFileNames)//.Take(limit)
			{
				var summaryData = File.ReadAllText(filePath);
				string fileName = Path.GetFileNameWithoutExtension(filePath);

				var blogPost = JsonConvert.DeserializeObject<BlogPost>(summaryData);
				if(blogPost.Tags.Contains(tag))
				{
					blogPost.Slug = fileName.Substring(11, fileName.Length - 11);
					var postBody = File.ReadAllText(filePath.Substring(0, filePath.Length-5) + ".md");
					blogPost.Body = TransformMarkdown(postBody);
					blogPost.Date = DateTime.Parse(fileName.Substring(0, 10));
					blogPost.TimeAgo = ToRelativeDate(blogPost.Date);

					if(blogPost.Date > DateTime.Now)
						continue;
					blogListings.Add(blogPost);
				}
			}
			return blogListings;
		}

		public BlogPost GetPost(string slug)
		{
			if(slug == null || slug.Contains("..") || slug.Contains("/") || slug.Contains("\\"))
				return null;

			var founds = Directory.GetFiles(PATH_BLOG_POSTS, "*" + slug + ".json", SearchOption.AllDirectories);
			if(founds.Length > 1)
				throw new Exception("More than 1 post found");
			else if(founds.Length==1)
				{
				var summaryData = File.ReadAllText(founds[0]);
				var postBody = File.ReadAllText(founds[0].Substring(0, founds[0].Length-5) + ".md");
				string fileName = Path.GetFileNameWithoutExtension(founds[0]);

				var blogPost = JsonConvert.DeserializeObject<BlogPost>(summaryData);
				blogPost.Slug = fileName.Substring(11, fileName.Length - 11);
				blogPost.Body = TransformMarkdown(postBody);
				blogPost.Date = DateTime.Parse(fileName.Substring(0, 10));
				blogPost.TimeAgo = ToRelativeDate(blogPost.Date);
				return blogPost;
			}

			return null;
		}

		public static string TransformMarkdown(string content)
		{
			//return new HeyRed.MarkdownSharp.Markdown().Transform(content);
			return CommonMark.CommonMarkConverter.Convert(content);
		}

        public static string TransformMarkdown2(string content)
        {
            var mkd = new MarkdownDeep.Markdown();
            return mkd.Transform(content);
        }


        public static string ToRelativeDate(DateTime dateTime)
		{
			var timeSpan = DateTime.Now - dateTime;

			if(timeSpan.TotalSeconds < 0)
				return "";

			if(timeSpan <= TimeSpan.FromSeconds(60))
				return string.Format("{0} seconds ago", timeSpan.Seconds);

			if(timeSpan <= TimeSpan.FromMinutes(60))
				return timeSpan.Minutes > 1 ? String.Format("{0} minutes ago", timeSpan.Minutes) : "a minute ago";

			if(timeSpan <= TimeSpan.FromHours(24))
				return timeSpan.Hours > 1 ? String.Format("{0} hours ago", timeSpan.Hours) : "an hour ago";

			if(timeSpan <= TimeSpan.FromDays(30))
				return timeSpan.Days > 1 ? String.Format("{0} days ago", timeSpan.Days) : "yesterday";

			if(timeSpan <= TimeSpan.FromDays(365))
				return timeSpan.Days > 60 ? String.Format("{0} months ago", timeSpan.Days / 30) : "a month ago";

			return timeSpan.Days > 365 ? String.Format("{0} years ago", timeSpan.Days / 365) : "a year ago";
		}
	}

	/*internal class MkdCode : HeyRed.MarkdownSharp.IMarkdownExtension
	{
		public string Transform(string text)
		{
			return text;
		}
	}*/
}