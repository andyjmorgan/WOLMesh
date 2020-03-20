using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using WOLMeshFrameworkHelpers;
using static WOLMeshTypes.Models;
//using NLog;
namespace WOLMeshClientService
{
    public partial class svcMain : ServiceBase
    {

        static WOLMeshFrameworkSignalRClient.ManagedClient _mc = null;
        static WOLMeshTypes.Models.NodeConfig _config = null;
        static DeviceIdentifier machineDetails = null;


        System.Timers.Timer _reconnectTimer = new System.Timers.Timer();
        public svcMain()
        {
            InitializeComponent();
        }

        static DeviceIdentifier GetMachineDetails()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Pulling Machine Details");
           DeviceIdentifier di = new DeviceIdentifier();
            NetworkDetails nd = new NetworkDetails();
            var globalProperties = System.Net.NetworkInformation.IPGlobalProperties.GetIPGlobalProperties();
            di.HostName = globalProperties.HostName;
            try
            {
                di.DomainName = System.DirectoryServices.ActiveDirectory.Domain.GetComputerDomain()?.Name ?? "unknown";

            }
            catch (Exception ex)
            {
                di.DomainName = "Unknown";
            }
            di.WindowsVersion = RegistryHelpers.GetMachineDetails();
            di.IsNetworkAvailable = NetworkInterface.GetIsNetworkAvailable();
            di.id = RegistryHelpers.GetMachineID();
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
                            System.Collections.Generic.IEnumerable<UnicastIPAddressInformation> count = nicprops.UnicastAddresses.Where(x => x.IsDnsEligible && (!x.Address.IsIPv6LinkLocal && !x.Address.IsIPv6Multicast && !x.Address.IsIPv6SiteLocal && !x.Address.IsIPv6Teredo));
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
            NLog.LogManager.GetCurrentClassLogger().Info("Latest Info: {0}", JsonConvert.SerializeObject(di, Formatting.Indented));
            return di;
        }

        static void AddressChangedCallback(object sender, EventArgs e)
        {

            NLog.LogManager.GetCurrentClassLogger().Info(" ---- Interface change detected ----");
            machineDetails = GetMachineDetails();

            //NLog.LogManager.GetCurrentClassLogger().Info(JsonConvert.SerializeObject(machineDetails, Formatting.Indented));

            if(machineDetails.AccessibleNetworks.Count > 0)
            {
                if (_mc.isConnected)
                {
                    _mc.RegisterSelf(machineDetails);
                }
            }        
        }

        public void SinkSSLErrors()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Sinking SSL Errors.");

            ServicePointManager
    .ServerCertificateValidationCallback +=
    (sender, cert, chain, sslPolicyErrors) => true;
        }
        

        protected override void OnStart(string[] args)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("---Service Starting---");
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);
            NLog.LogManager.GetCurrentClassLogger().Info("Args: {0}", JsonConvert.SerializeObject(args, Formatting.Indented));
            System.IO.Directory.SetCurrentDirectory(System.AppDomain.CurrentDomain.BaseDirectory);
            var cd = System.IO.Directory.GetCurrentDirectory();
            
            NLog.LogManager.GetCurrentClassLogger().Info("Current Directory: {0}", cd);

            var configFile = cd + "\\" + "nodeconfig.json";
            if (System.IO.File.Exists(configFile))
            {
                NLog.LogManager.GetCurrentClassLogger().Info("Reading configuration file: {0}", configFile);
                _config = JsonConvert.DeserializeObject<WOLMeshTypes.Models.NodeConfig>(System.IO.File.ReadAllText(configFile));
                NLog.LogManager.GetCurrentClassLogger().Info("Read configuration file successfully");
            }
            else
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Service configuration json file missing");
                WOLMeshTypes.Models.NodeConfig nc = new WOLMeshTypes.Models.NodeConfig();
                System.IO.File.WriteAllText(configFile,JsonConvert.SerializeObject(nc, Formatting.Indented));
                throw new Exception("Configuration file missing or empty");
            }

            NLog.LogManager.GetCurrentClassLogger().Info("Requesting Machine Info");
            machineDetails = GetMachineDetails();
            _reconnectTimer = new System.Timers.Timer();
            _reconnectTimer.Interval = _config.timerInterval * 1000;
            _reconnectTimer.Elapsed += _reconnectTimer_Elapsed;
            _reconnectTimer_Elapsed(null, null);
            _reconnectTimer.Start();

            if (_config.ignoreSSLErrors)
            {
                SinkSSLErrors();
            }

            //LogManager.GetCurrentClassLogger().Info("Service Starting");

        }

        static async Task<bool> OpenConnection(string url)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Opening Connection");

            try
            {
                _mc = new WOLMeshFrameworkSignalRClient.ManagedClient(_config.serveraddress);
                NLog.LogManager.GetCurrentClassLogger().Info("Attempting to connect to: " + _config.serveraddress);
                if (await _mc.OpenConnection())
                {
                    NLog.LogManager.GetCurrentClassLogger().Info("Connected");
                    var registrationResult = await _mc.RegisterSelf(machineDetails);
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

        private void _reconnectTimer_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            NLog.LogManager.GetCurrentClassLogger().Debug("Timer Ticking");

            if (_mc == null)
            {
                NLog.LogManager.GetCurrentClassLogger().Info("Managed Connection null, opening a connection");
                OpenConnection("https://localhost:5001/wolmeshhub");
            }
            else
            {
                if (_mc.isDisposed)
                {
                    NLog.LogManager.GetCurrentClassLogger().Info("Managed Connection disposed, opening a connection");

                    OpenConnection("https://localhost:5001/wolmeshhub");
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

                    OpenConnection("https://localhost:5001/wolmeshhub");
                }
            }
        }

        protected override void OnStop()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("---Service Stopping---");

        }
        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Power Change: {0}", JsonConvert.SerializeObject(powerStatus, Formatting.Indented));

            return base.OnPowerEvent(powerStatus);
        }
        protected override void OnSessionChange(SessionChangeDescription changeDescription)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Session Change: {0}", JsonConvert.SerializeObject(changeDescription, Formatting.Indented));
            base.OnSessionChange(changeDescription);
        }

        public static async void WakeUpCallReceived(object sender, WakeUpCall wakeup)
        {
             NLog.LogManager.GetCurrentClassLogger().Info("Awaking: " + Newtonsoft.Json.JsonConvert.SerializeObject(wakeup, Formatting.Indented));
            await WOLMeshFrameworkSignalRClient.WOL.WakeOnLan(wakeup, machineDetails);
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
