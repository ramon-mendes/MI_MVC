using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using System.Net.WebSockets;

namespace IOTEnergiaCore
{
    public class Startup
    {
		public static IServiceProvider ServiceProvider { get; set; }

		public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add framework services.
            services.AddMvc();

			//var cs = "Server=tcp:iotenergiadb.database.windows.net,1433;Initial Catalog=IOTEnergiaCore;Persist Security Info=False;User ID=midiway;Password=)3XI5Nio3$;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;";
			//services.AddDbContext<EnergiaContext>(options => options.UseSqlServer(cs));
		}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
			ServiceProvider = app.ApplicationServices;

			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if(env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
			app.UseWebSockets(new WebSocketOptions()
			{
				KeepAliveInterval = TimeSpan.FromSeconds(120),
			});

			app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

			var ws = new EnergiaWS();
			app.Use(async (context, next) =>
			{
				if(context.Request.Path == "/EnergyWS")
				{
					if(context.WebSockets.IsWebSocketRequest)
					{
						WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();
						await ws.HandleRequest(context, webSocket);
					}
					else
					{
						context.Response.StatusCode = 400;
					}
				}
				else
				{
					await next();
				}
			});
		}
    }
}
