using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GameService.ServiceCore;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GameService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var host = CreateWebHostBuilder(args).Build();

            // Find the ports and IPs it bound to.
            foreach (var address in host.ServerFeatures.Get<IServerAddressesFeature>().Addresses)
            {
                string fixedAddr = address.Replace("http://", "");
                if(fixedAddr.EndsWith("/"))
                {
                    fixedAddr = fixedAddr.Substring(0, fixedAddr.Length - 1);
                }
                Logger.Info($"Server bound to {fixedAddr}");
                Utils.SetServiceLocalAddress(fixedAddr);
            }
            
            // And run the server.
            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
