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
        static DaemonNodeConfig _nc = new DaemonNodeConfig();


        static void AddressChangedCallback(object sender, EventArgs e)
        {

            NLog.LogManager.GetCurrentClassLogger().Info(" ---- Interface change detected ----");
            _di = WOLMeshCoreSignalRClient.CoreHelpers.GetMachineDetails(_nc.UUID);

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

            WOLMeshCoreSignalRClient.CoreHelpers.OutputMachineDetails();
            _nc = WOLMeshCoreSignalRClient.CoreHelpers.GetNodeConfig();
            if (string.IsNullOrEmpty(_nc.serveraddress))
            {
                throw new Exception("You must specify the nodeconfig.json server address");
            }
            _di = WOLMeshCoreSignalRClient.CoreHelpers.GetMachineDetails(_nc.UUID);
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
                _mc = new ManagedClient(_nc.serveraddress, _nc.ignoreSSLErrors);
                NLog.LogManager.GetCurrentClassLogger().Info("Attempting to connect to: " + _nc.serveraddress);
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
            var machineDetails = WOLMeshCoreSignalRClient.CoreHelpers.GetMachineDetails(_nc.UUID);
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
