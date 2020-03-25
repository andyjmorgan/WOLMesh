using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using WOLMeshTypes;
using static WOLMeshTypes.Models;

namespace WOLMeshCoreSignalRClient
{
    public class CoreHelpers
    {
        public static DaemonNodeConfig GetNodeConfig()
        {

            DaemonNodeConfig _nc = new DaemonNodeConfig();
            var ConfigPath = Directory.GetCurrentDirectory() + System.IO.Path.DirectorySeparatorChar + @"NodeConfig.json";

            NLog.LogManager.GetCurrentClassLogger().Info("Looking for machine config here: {0}", ConfigPath);


            if (System.IO.File.Exists(ConfigPath))
            {

                _nc = JsonConvert.DeserializeObject<DaemonNodeConfig>(File.ReadAllText(ConfigPath));
            }
            else
            {
                System.IO.File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(_nc, Formatting.Indented));
            }

            if (string.IsNullOrEmpty(_nc.UUID))
            {
                NLog.LogManager.GetCurrentClassLogger().Warn("Setting new UUID as it's empty");
                _nc.UUID = Guid.NewGuid().ToString();
            }
            NLog.LogManager.GetCurrentClassLogger().Info("Node Config: {0}", JsonConvert.SerializeObject(_nc, Formatting.Indented));
            return _nc;
        }
        public static DeviceIdentifier GetMachineDetails(string ID)
        {

            DeviceIdentifier di = new DeviceIdentifier();
            NetworkDetails nd = new NetworkDetails();
            var globalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
            di.HostName = globalProperties.HostName;
            di.id = ID;
            di.DomainName = "N/A";
            di.WindowsVersion = "Unknown";
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.FreeBSD)) { di.WindowsVersion = "FreeBSD"; }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)) { di.WindowsVersion = "Linux"; }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) { di.WindowsVersion = "OSX"; }
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) { di.WindowsVersion = "Windows (Core)"; }
            //di.WindowsVersion = System.Runtime.InteropServices.RuntimeInformation.OSDescription;
            di.IsNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
            NetworkInterface[] nics = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

            var trimmedNics = nics.Where(x => x.OperationalStatus == OperationalStatus.Up && x.NetworkInterfaceType == NetworkInterfaceType.Ethernet).ToList();


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
                            System.Collections.Generic.IEnumerable<UnicastIPAddressInformation> count = nicprops.UnicastAddresses.Where(
                                x =>
                                (!x.Address.IsIPv6LinkLocal &&
                                !x.Address.IsIPv6Multicast &&
                                !x.Address.IsIPv6SiteLocal &&
                                x.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                                !x.Address.IsIPv6Teredo));
                            if (count.Count() > 0)
                            {
                                foreach (var c in count)
                                {
                                    var ips = new IPSegment(c.Address.ToString(), c.IPv4Mask.ToString());

                                    di.AccessibleNetworks.Add(new NetworkDetails
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
            return di;
        }

        public static void OutputMachineDetails()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("AppDomain Directory: {0}", AppDomain.CurrentDomain.BaseDirectory);
            NLog.LogManager.GetCurrentClassLogger().Info("Current Directory: {0}", System.IO.Directory.GetCurrentDirectory());
            System.IO.Directory.SetCurrentDirectory(AppDomain.CurrentDomain.BaseDirectory);
            NLog.LogManager.GetCurrentClassLogger().Info("New Current Directory: {0}", System.IO.Directory.GetCurrentDirectory());
            NLog.LogManager.GetCurrentClassLogger().Info("Working Directory: {0}", System.IO.Directory.GetCurrentDirectory());
            NLog.LogManager.GetCurrentClassLogger().Info("Framework: {0}", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            NLog.LogManager.GetCurrentClassLogger().Info("Architecture: {0}", System.Runtime.InteropServices.RuntimeInformation.OSArchitecture);
            NLog.LogManager.GetCurrentClassLogger().Info("ProcessArchitecture: {0}", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);
            NLog.LogManager.GetCurrentClassLogger().Info("OSDescription: {0}", System.Runtime.InteropServices.RuntimeInformation.OSDescription);
            NLog.LogManager.GetCurrentClassLogger().Info("Is FreeBSD: {0}", System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.FreeBSD));
            NLog.LogManager.GetCurrentClassLogger().Info("Is Linux: {0}", System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux));
            NLog.LogManager.GetCurrentClassLogger().Info("Is OSX: {0}", System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX));
            NLog.LogManager.GetCurrentClassLogger().Info("Is Windows: {0}", System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows));
            NLog.LogManager.GetCurrentClassLogger().Info("OS Description: {0}", System.Runtime.InteropServices.RuntimeInformation.OSDescription);
        }
    }
}
