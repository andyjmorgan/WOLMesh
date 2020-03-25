using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace WOLMeshCoreSignalRClient
{



    public class ManagedClient
    {

        #region Events


        public event EventHandler<string> ConnectionClosed;
        public event EventHandler<string> ConnectionReconnecting;
        public event EventHandler<string> ConnectionReconnected;
        public event EventHandler<string> ConnectionConnected;


        public event EventHandler<string> MessageReceived;
        public event EventHandler<WOLMeshTypes.Models.WakeUpCall> WakeUp;

        public bool isConnected = false;
        public bool isConnecting = false;
        public bool isDisposed = false;

        #endregion Events
        public string URL { get; set; }
        public bool IgnoreSSLCert { get; set; }

        private HubConnection _connection;


        public ManagedClient(string _URL, bool _sslCheck)
        {
            URL = _URL;
            IgnoreSSLCert = _sslCheck;
        }

        public async Task<bool> OpenConnection()
        {


            isConnecting = true;
            try
            {
                
                if (IgnoreSSLCert)
                {
                    _connection = new HubConnectionBuilder().WithUrl(String.Format("{0}/WOLMeshHub", URL), (opts) =>
                    {
                        opts.HttpMessageHandlerFactory = (message) =>
                        {
                            if (message is HttpClientHandler clientHandler)
                                // bypass SSL certificate
                                clientHandler.ServerCertificateCustomValidationCallback +=
                                     (sender, certificate, chain, sslPolicyErrors) => { return true; };
                            return message;
                        };
                    }).
               Build();
                }
                else
                {
                    _connection = new HubConnectionBuilder().WithUrl(URL).Build();

                }
                //_connection.
                NLog.LogManager.GetCurrentClassLogger().Info("Attempting to open a SignalR connection to: {0}", URL);
                _connection.On<string, string>("ReceiveMessage", OnMessageReceived);
                _connection.On<WOLMeshTypes.Models.WakeUpCall>("WakeUp", OnWakeUpCall);
                _connection.Reconnecting += _connection_Reconnecting;
                _connection.Reconnected += _connection_Reconnected;
                await _connection.StartAsync();
                NLog.LogManager.GetCurrentClassLogger().Info("SignalR connection state: {0}", _connection.State.ToString());

                if (_connection.State == HubConnectionState.Connected)
                {
                    ConnectionConnected?.Invoke(this, URL);
                    isConnected = true;
                    isConnecting = false;
                    _connection.Closed += _connection_Closed;
                    return true;
                }
                else
                {
                    isConnecting = false;
                    return false;
                }
            }
            catch (Exception ex)
            {
                isConnecting = false;
                NLog.LogManager.GetCurrentClassLogger().Warn("Failed to open a SignalR connection to: {0}. With Exception: {1}", URL, ex.ToString());
                return false;
            }
        }

        private async Task<bool> _connection_Reconnected(string arg)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("SignalRClient - [{0}] Reconnect Event: {1}", URL, arg);
            isConnected = true;
            ConnectionReconnected?.Invoke(this, arg);
            return true;
        }

        public void Dispose()
        {
            isConnected = false;
            if (_connection.State != HubConnectionState.Disconnected)
            {
                _connection.StopAsync();
            }
            try
            {
                _connection.DisposeAsync();
            }
            catch
            {

            }
            isDisposed = true;
        }

        private async Task<bool> _connection_Closed(Exception arg)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("SignalRClient - [{0}] Closed Event: {1}", URL, (arg?.ToString() ?? ""));

            isConnected = false;
            try
            {
                await _connection.StopAsync();
                ConnectionClosed?.Invoke(this, arg.ToString());
            }
            catch
            {
            }
            return true;


        }

        private async Task<bool> _connection_Reconnecting(Exception arg)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("SignalRClient - [{0}] Reconnecting Event: {1}", URL, (arg?.ToString() ?? ""));

            isConnected = false;
            ConnectionReconnecting?.Invoke(this, arg.ToString());
            return true;
        }


        public async Task<bool> RegisterSelf(WOLMeshTypes.Models.DeviceIdentifier deviceDetails)
        {
            try
            {
                await _connection.SendAsync("RegisterMachine", deviceDetails);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        public async Task<bool> UpdateUser(string userName)
        {
            try
            {
                await _connection.SendAsync("UpdateUser", userName);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public async Task<bool> SetMachineState(string userName)
        {
            try
            {
                await _connection.SendAsync("UpdateUser", userName);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        public async void SendHeartBeat()
        {
            try
            {
                if (isConnected)
                {

                    await _connection.SendAsync("HeartBeat");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private void OnMessageReceived(string user, string message)
        {
            MessageReceived?.Invoke(this, message);
            Console.WriteLine("ReceiveMessage - " + "User: " + user + " - Message: " + message);
        }

        private void OnWakeUpCall(WOLMeshTypes.Models.WakeUpCall deviceDetails)
        {
            WakeUp?.Invoke(this, deviceDetails);
            //Console.WriteLine(deviceDetails.MacAddress);
        }

    }
}


