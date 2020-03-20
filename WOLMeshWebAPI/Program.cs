
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
        public static string GetWorkingDirectory()
        {
            return Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\";
        }
        public static void Main(string[] args)
        {
            var isService = !(Debugger.IsAttached || args.Contains("--console"));
            var isDebug = Debugger.IsAttached;
            NLog.LogManager.GetCurrentClassLogger().Info("<===================Service Starting====================>");
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
            string SettingsFilePath = Directory.GetCurrentDirectory() + @"\ServiceSettings.JSON";
            if (File.Exists(SettingsFilePath)){
                try
                {
                    var configString = System.IO.File.ReadAllText(SettingsFilePath);
                    Runtime.SharedObjects.ServiceConfiguration = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.ServiceSettings>(configString);

                }
                catch(Exception ex)
                {
                    Runtime.SharedObjects.ServiceConfiguration = new Models.ServiceSettings();
                }
            }
            NLog.LogManager.GetCurrentClassLogger().Info("Service Settings: " + Newtonsoft.Json.JsonConvert.SerializeObject(Runtime.SharedObjects.ServiceConfiguration, Newtonsoft.Json.Formatting.Indented));
            
            
            var webHostBuilder = CreateHostBuilder(args);
            var webHost = webHostBuilder.Build();
            //if (isService)
            //{
            //    webHost.RunAsService();
            //}
            //else
            //{
                webHost.Run();
            //}

        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
            
            
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseKestrel(options =>
                    {
                        string selfsignedPath = Directory.GetCurrentDirectory() + "\\selfsigned.pfx";
                        string selfsignedSSLCertPassword = "WOLMeshSSLCertPassword123!£$";

                        if (!System.IO.File.Exists(selfsignedPath))
                        {
                            NLog.LogManager.GetCurrentClassLogger().Warn("Creating new Self Signed Certificate: {0}", selfsignedPath);
                            var cert = buildSelfSignedServerCertificate(selfsignedSSLCertPassword);
                            var pfx = cert.Export(X509ContentType.Pfx, selfsignedSSLCertPassword);
                            File.WriteAllBytes(selfsignedPath, pfx);
                        }

                        options.Listen(IPAddress.Any, Runtime.SharedObjects.ServiceConfiguration.httpsPort,
                        ListenOptions =>
                        {
                            ListenOptions.UseHttps(selfsignedPath, selfsignedSSLCertPassword);
                        });
                    }).
                    UseStartup<Startup>();
                });

    }
}
