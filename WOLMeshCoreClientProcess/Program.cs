using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.NetworkInformation;
using static WOLMeshTypes.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net;
using System.Net.Http;
using WOLMeshTypes;
using System.Threading.Tasks;
using WOLMeshCoreSignalRClient;
using Newtonsoft.Json;

namespace WOLMeshCoreClientProcess
{
    class Program
    {
        static System.Timers.Timer _reconnectTmr = new System.Timers.Timer();
        static DeviceIdentifier _di = new DeviceIdentifier();
        static WOLMeshCoreSignalRClient.ManagedClient _mc = null;

        static string URL = "";
        static bool IgnoreSSL = false;
        static string machineID = Guid.NewGuid().ToString();
        static DeviceIdentifier GetMachineDetails()
        {

            DeviceIdentifier di = new DeviceIdentifier();
            NetworkDetails nd = new NetworkDetails();
            var globalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
            di.HostName = globalProperties.HostName;
            di.id = machineID;
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
            NLog.LogManager.GetCurrentClassLogger().Info(Newtonsoft.Json.JsonConvert.SerializeObject(di, Newtonsoft.Json.Formatting.Indented));
            return di;
        }



        static void AddressChangedCallback(object sender, EventArgs e)
        {

            NLog.LogManager.GetCurrentClassLogger().Info(" ---- Interface change detected ----");
            _di = GetMachineDetails();

            //NLog.LogManager.GetCurrentClassLogger().Info(JsonConvert.SerializeObject(machineDetails, Formatting.Indented));

            try
            {
                if (_di.AccessibleNetworks.Count > 0)
                {
                    if (_mc.isConnected)
                    {
                        _mc.RegisterSelf(_di);
                    }
                }
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Failed to update via the address callback: " + ex.ToString());
            }
        }

        static void Main(string[] args)
        {

            var ConfigPath = System.IO.Directory.GetCurrentDirectory() + @"\ID.json";

            if (System.IO.File.Exists(ConfigPath))
            {
                machineID = System.IO.File.ReadAllText(ConfigPath);
            }
            else
            {
                machineID = Guid.NewGuid().ToString();
                System.IO.File.WriteAllText(ConfigPath, machineID);
            }
            var urlArg = args.Where(x => x.ToLower().StartsWith("--url=")).FirstOrDefault();
            IgnoreSSL = args.Where(x => x.ToLower().StartsWith("--ignoressl")).Count() > 0;

            NLog.LogManager.GetCurrentClassLogger().Info("Working Directory: " + System.IO.Directory.GetCurrentDirectory());
            NLog.LogManager.GetCurrentClassLogger().Info("Framework: {0}", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);
            NLog.LogManager.GetCurrentClassLogger().Info("Architecture: {0}", System.Runtime.InteropServices.RuntimeInformation.OSArchitecture);
            NLog.LogManager.GetCurrentClassLogger().Info("ProcessArchitecture: {0}", System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture);
            NLog.LogManager.GetCurrentClassLogger().Info("OSDescription: {0}", System.Runtime.InteropServices.RuntimeInformation.OSDescription);
            NLog.LogManager.GetCurrentClassLogger().Info("Is FreeBSD: {0}", System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.FreeBSD));
            NLog.LogManager.GetCurrentClassLogger().Info("Is Linux: {0}", System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux));
            NLog.LogManager.GetCurrentClassLogger().Info("Is OSX: {0}", System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX));
            NLog.LogManager.GetCurrentClassLogger().Info("Is Windows: {0}", System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows));
            NLog.LogManager.GetCurrentClassLogger().Info("OS Description: {0}", System.Runtime.InteropServices.RuntimeInformation.OSDescription);
            
            _di = GetMachineDetails();


            if (urlArg == null)
            {
                throw new Exception("no --url arg passed of the url is not valid. the correct argument is --url=https://example:7443");
            }


            URL = urlArg.Split("=")[1];

            //NLog.LogManager.GetCurrentClassLogger().Info("Attempting to Connect to {0}", url);
            //var _connection = new HubConnectionBuilder().WithUrl(String.Format("{0}/WOLMeshHub", urlArg.Split("=")[1]), (opts) =>
            // {
            //     opts.HttpMessageHandlerFactory = (message) =>
            //     {
            //         if (message is HttpClientHandler clientHandler)
            //            // bypass SSL certificate
            //            clientHandler.ServerCertificateCustomValidationCallback +=
            //                 (sender, certificate, chain, sslPolicyErrors) => { return true; };
            //         return message;
            //     };
            // }).
            //    Build();
            ////_connection.


            //_connection.StartAsync().Wait();

            //NLog.LogManager.GetCurrentClassLogger().Info("Connection state: {0}", _connection.State.ToString());
            //if (_connection.State == HubConnectionState.Connected)
            //{
            //    try
            //    {
            //        NLog.LogManager.GetCurrentClassLogger().Info("Registering Local Device");
            //        _connection.SendAsync("RegisterMachine", md);

            //    }
            //    catch (Exception ex)
            //    {
            //        NLog.LogManager.GetCurrentClassLogger().Info(ex.ToString());

            //    }
            //}

            _reconnectTmr = new System.Timers.Timer();
            _reconnectTmr.Interval = 30 * 1000;
            _reconnectTmr.Elapsed += _reconnectTimer_Elapsed;
            _reconnectTimer_Elapsed(null, null);
            _reconnectTmr.Start();

            Console.Title= "WOLMesh Agent - Press ctrl + c to exit.";
            Console.ReadLine();
        }



        static async Task<bool> OpenConnection()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Opening Connection");

            try
            {
                _mc = new ManagedClient(URL, IgnoreSSL);
                NLog.LogManager.GetCurrentClassLogger().Info("Attempting to connect to: " + URL);
                if (await _mc.OpenConnection())
                {
                    NLog.LogManager.GetCurrentClassLogger().Info("Connected");
                    var registrationResult = await _mc.RegisterSelf(_di);
                    NLog.LogManager.GetCurrentClassLogger().Info("Registering");

                    if (registrationResult)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Info("Registered");

                        _mc.ConnectionClosed += OnDisconnect;
                        _mc.ConnectionReconnected += onReconnected;
                        _mc.ConnectionReconnecting += OnReconnecting;
                        _mc.ConnectionConnected += OnConnected;
                        _mc.WakeUp += WakeUpCallReceived;
                        return true;
                    }
                    else
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error("Failed To Register");
                        return false;

                    }
                }
                else
                {
                    NLog.LogManager.GetCurrentClassLogger().Warn("Failed To Connect");
                    return false;
                }
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Exception while connecting: " + ex.ToString());
                return false;
            }

        }
        static void DisposeConnection()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Disposing Connection");
            _mc.Dispose();
            _mc.ConnectionClosed -= OnDisconnect;
            _mc.ConnectionReconnected -= onReconnected;
            _mc.ConnectionReconnecting -= OnReconnecting;
            _mc.ConnectionConnected -= OnConnected;
            _mc.WakeUp -= WakeUpCallReceived;
            _mc = null;
        }

        static void _reconnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
           

            if (_mc == null)
            {
                NLog.LogManager.GetCurrentClassLogger().Info("Managed Connection null, opening a connection");
                OpenConnection();
            }
            else
            {
                if (_mc.isDisposed)
                {
                    NLog.LogManager.GetCurrentClassLogger().Info("Managed Connection disposed, opening a connection");

                    OpenConnection();
                }
                else if (_mc.isConnecting)
                {
                    NLog.LogManager.GetCurrentClassLogger().Info("Managed Connection connecting");


                    //wait
                }
                else if (_mc.isConnected)
                {
                    NLog.LogManager.GetCurrentClassLogger().Debug("Managed Connection connected, sending heartbeat");

                    _mc.SendHeartBeat();
                }
                else
                {
                    NLog.LogManager.GetCurrentClassLogger().Debug("Managed Connection presumed disconnected, opening connection.");

                    OpenConnection();
                }
            }
        }
      

      


        public static async void WakeUpCallReceived(object sender, WakeUpCall wakeup)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Awaking: " + Newtonsoft.Json.JsonConvert.SerializeObject(wakeup, Formatting.Indented));
            await WOL.WakeOnLan(wakeup, _di.AccessibleNetworks);
        }
        public static void OnDisconnect(object sender, string error)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("OnDisconnect");
            DisposeConnection();
        }
        public static void OnReconnecting(object sender, string message)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("OnReconnecting");

        }
        public static void OnConnected(object sender, string message)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("OnConnected:");

        }
        public static void onReconnected(object sender, string message)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("onReconnected: {0}", message);
            var machineDetails = GetMachineDetails();
            try
            {
                _mc.RegisterSelf(machineDetails).Wait();
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Exception caught while registering myself: " + ex.ToString());
            }


        }
    }
}
