using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using WOLMeshCoreSignalRClient;
using WOLMeshTypes;
using static WOLMeshTypes.Models;

namespace WOLMeshClientDaemon
{
    public class Worker : BackgroundService
    {
        static DaemonNodeConfig _dnc = new DaemonNodeConfig();
        public Worker(DaemonNodeConfig _nc)
        {
            _dnc = _nc;
        }
        static System.Timers.Timer _reconnectTmr = new System.Timers.Timer();
        static DeviceIdentifier _di = new DeviceIdentifier();
        static WOLMeshCoreSignalRClient.ManagedClient _mc = null;

        static string machineID = Guid.NewGuid().ToString();

        static void AddressChangedCallback(object sender, EventArgs e)
        {

            NLog.LogManager.GetCurrentClassLogger().Info(" ---- Interface change detected ----");
            _di = CoreHelpers.GetMachineDetails(_dnc.UUID);

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
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _di = CoreHelpers.GetMachineDetails(_dnc.UUID);
            _reconnectTmr = new System.Timers.Timer();
            _reconnectTmr.Interval = _dnc.timerInterval * 1000;
            _reconnectTmr.Elapsed += _reconnectTimer_Elapsed;
            _reconnectTimer_Elapsed(null, null);
            _reconnectTmr.Start();
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);
            while (!stoppingToken.IsCancellationRequested)
            {
                NLog.LogManager.GetCurrentClassLogger().Trace("Worker running at: {0}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
            _reconnectTmr.Stop();
            _reconnectTmr.Dispose();
            _mc.Dispose();
        }

        static async Task<bool> OpenConnection()
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Opening Connection");

            try
            {
                _mc = new ManagedClient(_dnc.serveraddress, _dnc.ignoreSSLErrors);
                NLog.LogManager.GetCurrentClassLogger().Info("Attempting to connect to: " + _dnc.serveraddress);
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
                        _mc.MachineConfigChange += _mc_MachineConfigChange;
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

        private static void _mc_MachineConfigChange(object sender, MachineConfig e)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Updated Machine Settings received: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(e));
            _reconnectTmr.Interval = e.HeartBeatIntervalSeconds * 1000;
            _dnc.timerInterval = e.HeartBeatIntervalSeconds;
            _dnc.maxPackets = e.MaxPackets;
            WOLMeshCoreSignalRClient.CoreHelpers.SaveNodeConfig(_dnc);
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
            _mc.MachineConfigChange -= _mc_MachineConfigChange;
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
            NLog.LogManager.GetCurrentClassLogger().Info("Awaking: " + JsonConvert.SerializeObject(wakeup, Formatting.Indented));
            await WOL.WakeOnLan(wakeup, _di.AccessibleNetworks,_dnc.maxPackets);
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
            var machineDetails = CoreHelpers.GetMachineDetails(_dnc.UUID);
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
