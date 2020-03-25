using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using static WOLMeshTypes.Models;

namespace WOLMeshClientDaemon
{
    public class Program
    {
        static DaemonNodeConfig _nc = new DaemonNodeConfig();

        public static void Main(string[] args)
        {
            
            WOLMeshCoreSignalRClient.CoreHelpers.OutputMachineDetails();
            _nc = WOLMeshCoreSignalRClient.CoreHelpers.GetNodeConfig();

            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>(provider =>
                   {

                      return new Worker(_nc);

                   });
                });

        public static IHostBuilder CreateSystemDHostBuilder(string[] args) =>
           Host.
            CreateDefaultBuilder(args).
            UseSystemd().
               ConfigureServices((hostContext, services) =>
               {
                   services.AddHostedService<Worker>(provider =>
                   {

                       return new Worker(_nc);

                   });
               });

        public static IHostBuilder CreateWindowsServiceHostBuilder(string[] args) =>
          Host.
            CreateDefaultBuilder(args).
            UseWindowsService().
            ConfigureServices((hostContext, services) =>
              {
                  services.AddHostedService<Worker>(provider =>
                  {

                      return new Worker(_nc);

                  });
              });
    }
}
