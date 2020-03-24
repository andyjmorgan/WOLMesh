
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting.WindowsServices;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Net.NetworkInformation;
using static WOLMeshTypes.Models;
using WOLMeshTypes;
using Newtonsoft.Json;

namespace WOLMeshWebAPI
{
    public class Program
    {

        private static X509Certificate2 buildSelfSignedServerCertificate(string password)
        {
            SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
            sanBuilder.AddIpAddress(IPAddress.Loopback);
            sanBuilder.AddIpAddress(IPAddress.IPv6Loopback);
            sanBuilder.AddDnsName("localhost");
            sanBuilder.AddDnsName(Environment.MachineName);

            X500DistinguishedName distinguishedName = new X500DistinguishedName($"CN={Environment.MachineName}");

            using (RSA rsa = RSA.Create(2048))
            {
                var request = new CertificateRequest(distinguishedName, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                request.CertificateExtensions.Add(
                    new X509KeyUsageExtension(X509KeyUsageFlags.DataEncipherment | X509KeyUsageFlags.KeyEncipherment | X509KeyUsageFlags.DigitalSignature, false));


                request.CertificateExtensions.Add(
                   new X509EnhancedKeyUsageExtension(
                       new OidCollection { new Oid("1.3.6.1.5.5.7.3.1") }, false));

                request.CertificateExtensions.Add(sanBuilder.Build());

                var certificate = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));
                certificate.FriendlyName = "WOLMesh";

                return new X509Certificate2(certificate.Export(X509ContentType.Pfx, password), password, X509KeyStorageFlags.Exportable);
            }
        }

        static List<NetworkDetails> GetNetworkDetails()
        {

            List<NetworkDetails> localNetworks = new List<NetworkDetails>();
            try
            {
                NLog.LogManager.GetCurrentClassLogger().Info("Pulling Server Details");
                var globalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
                NetworkInterface[] nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                NLog.LogManager.GetCurrentClassLogger().Debug("All Nics: {0}", nics.Count());
                var trimmedNics = nics.Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType == NetworkInterfaceType.Ethernet).ToList();
                NLog.LogManager.GetCurrentClassLogger().Debug("Trimmed Nics: {0}", trimmedNics.Count());
                foreach (var nic in trimmedNics)
                {
                    var physicalAddress = nic.GetPhysicalAddress();
                    var nicprops = nic.GetIPProperties();

                    if (nic.Supports(NetworkInterfaceComponent.IPv4))
                    {
                        if (nicprops.GatewayAddresses.Count > 0)
                        {
                            if (nicprops.UnicastAddresses?.Count > 0)
                            {
                                System.Collections.Generic.IEnumerable<UnicastIPAddressInformation> count = nicprops.UnicastAddresses.Where(x =>
                                !x.Address.IsIPv6LinkLocal &&
                                !x.Address.IsIPv6Multicast &&
                                !x.Address.IsIPv6SiteLocal &&
                                x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                !x.Address.IsIPv6Teredo
                                );
                                if (count.Count() > 0)
                                {
                                    foreach (var c in count)
                                    {
                                        var ips = new IPSegment(c.Address.ToString(), c.IPv4Mask.ToString());

                                        localNetworks.Add(new NetworkDetails
                                        {
                                            IPAddress = c.Address.ToString(),
                                            MacAddress = physicalAddress.ToString(),
                                            BroadcastAddress = IpHelpers.ToIpString(ips.BroadcastAddress),
                                            SubnetMask = c.IPv4Mask.ToString(),
                                        });
                                    }

                                }
                            }
                        }
                    }
                }                
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Exception caught in getNetworkDetails: {0}", ex.ToString());
            }
            NLog.LogManager.GetCurrentClassLogger().Info("Latest  Network Info: {0}", JsonConvert.SerializeObject(localNetworks, Formatting.Indented));
            return localNetworks;
        }


        static void AddressChangedCallback(object sender, EventArgs e)
        {

            NLog.LogManager.GetCurrentClassLogger().Info(" ---- Interface change detected ----");
           

            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    NLog.LogManager.GetCurrentClassLogger().Info("Network is available, checking interfaces and networks.");

                    Runtime.SharedObjects.localNetworks = GetNetworkDetails();
                }
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Failed to update via the address callback: " + ex.ToString());
            }
        }


        public static string GetWorkingDirectory()
        {
            return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\";
        }
        public static void Main(string[] args)
        {
            var preColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("If you can read this, you are running the service as a console application, this application will need admin rights to bind to the port");
            Console.ForegroundColor = preColor;


            var isService = !(Debugger.IsAttached || args.Contains("--console"));
            var isDebug = Debugger.IsAttached;
            NLog.LogManager.GetCurrentClassLogger().Info("<===================Process Starting====================>");
            NLog.LogManager.GetCurrentClassLogger().Info("Machine Name: {0}", Environment.MachineName);
            NLog.LogManager.GetCurrentClassLogger().Info("Environment is x64: {0}", Environment.Is64BitOperatingSystem);
            NLog.LogManager.GetCurrentClassLogger().Info("Process is X64: {0}", Environment.Is64BitProcess);
            NLog.LogManager.GetCurrentClassLogger().Info("Processor Count: {0}", Environment.ProcessorCount);
            NLog.LogManager.GetCurrentClassLogger().Info("<=======================================================>");
            NLog.LogManager.GetCurrentClassLogger().Info("Working directory is: {0}", GetWorkingDirectory());
            NLog.LogManager.GetCurrentClassLogger().Info("Current directory is: {0}", Directory.GetCurrentDirectory());

            if (!isDebug)
            {
                NLog.LogManager.GetCurrentClassLogger().Info("Assumed working directory: " + Directory.GetCurrentDirectory());
                Directory.SetCurrentDirectory(GetWorkingDirectory());
                NLog.LogManager.GetCurrentClassLogger().Info("Current working directory: " + Directory.GetCurrentDirectory());
            }
            Runtime.SharedObjects.LoadConfig();
            NLog.LogManager.GetCurrentClassLogger().Info("Service Settings: " + Newtonsoft.Json.JsonConvert.SerializeObject(Runtime.SharedObjects.ServiceConfiguration, Newtonsoft.Json.Formatting.Indented));

            Runtime.SharedObjects.localNetworks = GetNetworkDetails();

            ValidateCertificatePath();

            IHostBuilder webHostBuilder = CreateServiceHostBuilder(args);         
            var webHost = webHostBuilder.Build();
            webHost.Run();


        }


        public static void CreateSSLCert()
        {
            string selfsignedPath = Directory.GetCurrentDirectory() + "\\selfsigned.pfx";
            string selfsignedSSLCertPassword = "WOLMeshSSLCertPassword123!£$";

            if (System.IO.File.Exists(selfsignedPath))
            {
                System.IO.File.Delete(selfsignedPath);
            }
            if (!System.IO.File.Exists(selfsignedPath))
            {
                NLog.LogManager.GetCurrentClassLogger().Warn("Creating new Self Signed Certificate: {0}", selfsignedPath);
                var cert = buildSelfSignedServerCertificate(selfsignedSSLCertPassword);
                var pfx = cert.Export(X509ContentType.Pfx, selfsignedSSLCertPassword);
                File.WriteAllBytes(selfsignedPath, pfx);
                Runtime.SharedObjects.ServiceConfiguration.SSLCertificatePFXFilePath = selfsignedPath;
                Runtime.SharedObjects.ServiceConfiguration.SSLCertificatePFXPassword = selfsignedSSLCertPassword;
                Runtime.SharedObjects.SaveConfig();
            }

        }
        public static void ValidateCertificatePath()
        {
            NLog.LogManager.GetCurrentClassLogger().Debug("Validating SSL Certificate");
            if (string.IsNullOrEmpty(Runtime.SharedObjects.ServiceConfiguration.SSLCertificatePFXFilePath))
            {
                NLog.LogManager.GetCurrentClassLogger().Warn("No SSL Certificate specified, creating one.");

                CreateSSLCert();
            }
            else if (!File.Exists(Runtime.SharedObjects.ServiceConfiguration.SSLCertificatePFXFilePath))
            {
                NLog.LogManager.GetCurrentClassLogger().Warn("SSL Certificate does not exist, creating one.");
                CreateSSLCert();
            }
        }

        public static IHostBuilder CreateServiceHostBuilder(string[] args) =>


     Host.CreateDefaultBuilder(args).
     UseWindowsService().

     ConfigureWebHostDefaults(webBuilder =>
     {
         webBuilder.UseKestrel(options =>
         {
             ValidateCertificatePath();

             options.Listen(IPAddress.Any, Runtime.SharedObjects.ServiceConfiguration.httpsPort,
                    ListenOptions =>
                 {
                     ListenOptions.UseHttps(Runtime.SharedObjects.ServiceConfiguration.SSLCertificatePFXFilePath, Runtime.SharedObjects.ServiceConfiguration.SSLCertificatePFXPassword);
                 });
         }).
                UseStartup<Startup>();
     });

        public static IHostBuilder CreateHostBuilder(string[] args) =>


            Host.CreateDefaultBuilder(args).

            ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(options =>
                    {
                        ValidateCertificatePath();

                        options.Listen(IPAddress.Any, Runtime.SharedObjects.ServiceConfiguration.httpsPort,
                        ListenOptions =>
                        {
                            ListenOptions.UseHttps(Runtime.SharedObjects.ServiceConfiguration.SSLCertificatePFXFilePath, Runtime.SharedObjects.ServiceConfiguration.SSLCertificatePFXPassword);
                        });
                    }).
                    UseStartup<Startup>();
                });

    }
}
