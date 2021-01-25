using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

namespace MI_MVC
{
	public static class Utils
	{
#if DEBUG
		public const bool IsDebugBuild = true;
#else
		public const bool IsDebugBuild = false;
#endif

		public static readonly bool IsAzure = Environment.UserDomainName == "IIS APPPOOL";
		public static readonly bool IsUmbler = Environment.UserDomainName == "REDEHOST";
		public static readonly bool IsLocalHost = !IsAzure && !IsUmbler;

		public static string ServerName
		{
			get
			{
				if(IsLocalHost) return "localhost";
				if(IsAzure) return "Azure";
				if(IsUmbler) return "Kinghost";
				return "WHAT!!";
			}
		}

		public static void Ensure(Expression<Func<bool>> expr)
		{
			var f = expr.Compile();
			if(!f())
			{
				Debug.Assert(false);
				throw new Exception("ENSURE Failed: " + expr.ToString());
			}
		}

		public static void SendMail(MailMessage message)
		{
			SmtpClient smtp = new SmtpClient("smtp.umbler.com", 587);
			smtp.UseDefaultCredentials = false;
			smtp.Credentials = new NetworkCredential("ramon@misoftware.com.br", "SEnha123");
			smtp.Send(message);
		}

		public static void SendBootMail(string body, string subject, Attachment att = null)
		{
			MailMessage message = new MailMessage();
			message.To.Add("boot@misoftware.com.br");
			message.Subject = subject + " (" + ServerName + ")";
			message.From = new MailAddress("ramon@misoftware.com.br");
			message.Body = body;
			if(att != null)
				message.Attachments.Add(att);

			SendMail(message);
		}

		public static void SendTheMasterMail(string body, string subject)// messages are
		{
			MailMessage message = new MailMessage();
			message.To.Add("ramon@misoftware.com.br");
			message.Subject = subject + " (" + ServerName + ")";
			message.From = new MailAddress("ramon@misoftware.com.br");
			message.Body = body;

			SendMail(message);
		}

		public static void SendMailTo(string mailto, string subject, string body)
		{
			MailMessage message = new MailMessage();
			message.To.Add(mailto);
			message.Subject = subject;
			message.From = new MailAddress("ramon@misoftware.com.br");
			message.Body = body;

			SendMail(message);
		}

		public static void SendMailToHTML(string mailto, string subject, string html)
		{
			MailMessage message = new MailMessage();
			message.To.Add(mailto);
			message.Subject = subject;
			message.From = new MailAddress("ramon@misoftware.com.br");
			message.Body = html;
			message.IsBodyHtml = true;

			SendMail(message);
		}

		public static void SendMailLogException(Exception ex)
		{
#if DEBUG
			Debug.Assert(false);
#else
			MailMessage message = new MailMessage();
			message.To.Add("ramon@misoftware.com.br");
			message.Subject = "MI Software SITE - Exception (" + ServerName + ")";
			message.From = new MailAddress("ramon@misoftware.com.br");
			message.Body = ex.ToString();
			SendMail(message);
#endif
		}


		public static string Capitalize(this string str)
		{
			return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
		}

		public static DateTime NowBrazil()
		{
			return TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("E. South America Standard Time"));
		}

		public static int ToUnixEpoch(this DateTime dt)
		{
			int unixTimestamp = (int)(dt.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
			return unixTimestamp;
		}

		public static long ToUnixTime(this DateTime date)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return Convert.ToInt64((date - epoch).TotalSeconds);
		}

		public static DateTime FromUnixTime(this long unixTime)
		{
			var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			return epoch.AddSeconds(unixTime);
		}

		public static CookieContainer ReadCookies(this HttpResponseMessage response, CookieContainer cookieContainer)
		{
			var pageUri = response.RequestMessage.RequestUri;

			IEnumerable<string> cookies;
			if(response.Headers.TryGetValues("set-cookie", out cookies))
			{
				foreach(var c in cookies)
				{
					cookieContainer.SetCookies(pageUri, c);
				}
			}

			return cookieContainer;
		}

		public static void CatchAsMasterMail(Action a)
		{
			try
			{
				a();
			}
			catch(Exception ex)
			{
				SendMailLogException(ex);
			}
		}

		public static T RetryPattern<T>(Func<T> f, string error_msg)// throws after 10 attemps fails
		{
			Exception last_ex = null;
			for(int i = 0; i < 10; i++)
			{
				try
				{
					return f();
				}
				catch(Exception ex)
				{
					last_ex = ex;
					Thread.Sleep(TimeSpan.FromSeconds(2));
				}
			}
			throw new Exception(error_msg, last_ex);
		}

		public static Task<T> RetryPatternAsync<T>(Func<T> f, string error_msg)// throws after 10 attemps fails
		{
			return Task.Run(() =>
			{
				Exception last_ex = null;
				for(int i = 0; i < 10; i++)
				{
					try
					{
						return f();
					}
					catch(Exception ex)
					{
						last_ex = ex;
						Thread.Sleep(TimeSpan.FromSeconds(2));
					}
				}
				throw new Exception(error_msg, last_ex);
			});
		}

		public static string DumpObject(object obj)
		{
			if(obj == null)
				return "ERROR: obj is null";

			StringBuilder sb = new StringBuilder();
			foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
			{
				string name = descriptor.Name;
				object value = descriptor.GetValue(obj);
				Console.WriteLine("{0}={1}", name, value);
				sb.AppendLine(name + "=" + value);
			}
			return sb.ToString();
		}

		public static string RandomString(int length, string allowedChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789")
		{
			const int byteSize = 0x100;
			var allowedCharSet = new HashSet<char>(allowedChars).ToArray();
			if(byteSize < allowedCharSet.Length) throw new ArgumentException(String.Format("allowedChars may contain no more than {0} characters.", byteSize));

			// Guid.NewGuid and System.Random are not particularly random. By using a
			// cryptographically-secure random number generator, the caller is always
			// protected, regardless of use.
			using(var rng = new System.Security.Cryptography.RNGCryptoServiceProvider())
			{
				var result = new StringBuilder();
				var buf = new byte[128];
				while(result.Length < length)
				{
					rng.GetBytes(buf);
					for(var i = 0; i < buf.Length && result.Length < length; ++i)
					{
						// Divide the byte into allowedCharSet-sized groups. If the
						// random value falls into the last group and the last group is
						// too small to choose from the entire allowedCharSet, ignore
						// the value in order to avoid biasing the result.
						var outOfRangeStart = byteSize - (byteSize % allowedCharSet.Length);
						if(outOfRangeStart <= buf[i]) continue;
						result.Append(allowedCharSet[buf[i] % allowedCharSet.Length]);
					}
				}
				return result.ToString();
			}
		}

		public static byte[] ReadFully(this Stream input)
		{
			byte[] buffer = new byte[16 * 1024];
			using(MemoryStream ms = new MemoryStream())
			{
				int read;
				while((read = input.Read(buffer, 0, buffer.Length)) > 0)
				{
					ms.Write(buffer, 0, read);
				}
				return ms.ToArray();
			}
		}

		public static IEnumerable<string> CustomSort(this IEnumerable<string> list)// sort filenames in natural order (as in explorer)
		{
			int maxLen = list.Select(s => s.Length).Max();

			return list.Select(s => new
			{
				OrgStr = s,
				SortStr = Regex.Replace(s, @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, char.IsDigit(m.Value[0]) ? ' ' : '\xffff'))
			})
			.OrderBy(x => x.SortStr)
			.Select(x => x.OrgStr);
		}

		public static IEnumerable<string> CustomSortByFilename(this IEnumerable<string> list)// sort filenames in natural order (as in explorer)
		{
			int maxLen = list.Select(s => s.Length).Max();

			return list.Select(s => new
			{
				OrgStr = s,
				SortStr = Regex.Replace(Path.GetFileName(s), @"(\d+)|(\D+)", m => m.Value.PadLeft(maxLen, char.IsDigit(m.Value[0]) ? ' ' : '\xffff'))
			})
			.OrderBy(x => x.SortStr)
			.Select(x => x.OrgStr);
		}

		public static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

			if(!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if(!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach(FileInfo file in files)
			{
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, false);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if(copySubDirs)
			{
				foreach(DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}

		public static T DeserializeBSON<T>(this byte[] dataBSON)
		{
			using(MemoryStream ms = new MemoryStream(dataBSON))
			{
				using(BsonReader reader = new BsonReader(ms))
				{
					return new JsonSerializer().Deserialize<T>(reader);
				}
			}
		}

		public static byte[] SerializeBSON(this object data)
		{
			using(MemoryStream ms = new MemoryStream())
			{
				using(BsonWriter writer = new BsonWriter(ms))
				{
					new JsonSerializer().Serialize(writer, data);
					return ms.ToArray();
				}
			}
		}

		public static void EncryptBlock(byte[] lpvBlock, string szPassword)
		{
			int nPWLen = szPassword.Length;
			char[] lpsPassBuff = szPassword.ToCharArray();

			for(int nChar = 0, nCount = 0; nChar < lpvBlock.Length; nChar++)
			{
				char cPW = lpsPassBuff[nCount];
				lpvBlock[nChar] ^= (byte)cPW;
				lpsPassBuff[nCount] = (char)((cPW + 13) % 256);
				nCount = (nCount + 1) % nPWLen;
			}
		}

		private static Random rng = new Random();

		public static IList<T> Shuffle<T>(this IList<T> list)
		{
			int n = list.Count;
			while(n > 1)
			{
				n--;
				int k = rng.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
			return list;
		}
	}

	public class StopwatchAuto : Stopwatch
	{
		public StopwatchAuto()
		{
			Start();
		}

		public string StopAndLog(string what = null)
		{
			Stop();

			if(what == null)
			{
				StackTrace stackTrace = new StackTrace();
				what = stackTrace.GetFrame(1).GetMethod().Name + "()";
			}
			//Utils.DebugOutputString(what + " took " + ElapsedMilliseconds + "ms");
			return what + " took " + ElapsedMilliseconds + "ms";
		}

		/*public string StopAndLogRelease(string what = null)
		{
			Stop();

			if(what == null)
			{
				StackTrace stackTrace = new StackTrace();
				what = stackTrace.GetFrame(1).GetMethod().Name + "()";
			}
			Utils.ReleaseOutputString(what + " took " + ElapsedMilliseconds + "ms");
		}*/
	}

	public class AllowCrossSiteAttribute : ActionFilterAttribute
	{
		public override void OnActionExecuting(ActionExecutingContext filterContext)
		{
			filterContext.RequestContext.HttpContext.Response.AddHeader("Access-Control-Allow-Origin", "*");
			base.OnActionExecuting(filterContext);
		}
	}

	public class TimedWebClient : WebClient
	{
		// Timeout in milliseconds, default = 600,000 msec
		public int Timeout { get; set; }

		public TimedWebClient()
		{
			Timeout = 600000;
		}

		protected override WebRequest GetWebRequest(Uri address)
		{
			var objWebRequest = base.GetWebRequest(address);
			objWebRequest.Timeout = this.Timeout;
			return objWebRequest;
		}
	}

}