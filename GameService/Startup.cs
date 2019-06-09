using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;
using GameService.ServiceCore;
using GameService.WebsocketsHelpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GameService
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                options.MinimumSameSitePolicy = SameSiteMode.Lax;
            });

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();
            app.UseWebSockets();

            app.UseMvc();

            // try to find what address we are bound to. If we can we will let bots connect locally
            // if not we will direct them through the host name.
            try
            {
                // Find the ports and IPs it bound to.
                var serverAddressesFeature = app.ServerFeatures.Get<IServerAddressesFeature>();
                foreach (var address in serverAddressesFeature.Addresses)
                {
                    string fixedAddr = address.Replace("http://", "");
                    if (fixedAddr.EndsWith("/"))
                    {
                        fixedAddr = fixedAddr.Substring(0, fixedAddr.Length - 1);
                    }
                    Logger.Info($"Server bound to {fixedAddr}");
                    Utils.SetServiceLocalAddress(fixedAddr);
                }
            }
            catch(Exception e)
            {
                Logger.Error("Failed to find localy bound address.", e);
            }
        
            // This code handles websockets coming into the system.
            app.Use(async (context, next) =>
            {
                if (context.Request.Path == "/ws")
                {
                    if (context.WebSockets.IsWebSocketRequest)
                    {
                        // Upgrade the connection
                        WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync();

                        // We need to block this exectuion here until we are done with the socket.
                        // If we don't, the socket will close when it returns.
                        // The BetterWebsocket class will set this completion when it's done.
                        var tcs = new TaskCompletionSource<object>();
                        WebsocketManager.Get().NewConnection(webSocket, tcs);
                        await tcs.Task;
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
