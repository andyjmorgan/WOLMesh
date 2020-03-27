using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WOLMeshWebAPI.ViewModels;

namespace WOLMeshWebAPI.Hubs
{
    public class WOLMeshHub : Hub
    {
        DB.AppDBContext _context;
        public WOLMeshHub(DB.AppDBContext context)
        {
            _context = context;
        }

        public override Task OnConnectedAsync()
        {
            NLog.LogManager.GetCurrentClassLogger().Trace("New Connection, {0}", Context.ConnectionId);
            return base.OnConnectedAsync();
        }


        public override Task OnDisconnectedAsync(Exception exception)
        {

            if (exception != null)
            {
                NLog.LogManager.GetCurrentClassLogger().Trace("Session Disconnected, {0} with Error: {1}", Context.ConnectionId, exception.ToString());
                Runtime.SharedObjects.RemoveHubConnection(Context.ConnectionId, exception.ToString());
            }
            else
            {
                NLog.LogManager.GetCurrentClassLogger().Trace("Session Disconnected, {0}", Context.ConnectionId);
                Runtime.SharedObjects.RemoveHubConnection(Context.ConnectionId);
            }
            //var RemoveList = Runtime.SharedObjects.connections.Connections.Where(x => x.ConnectionID == Context.ConnectionId).ToList();
            return base.OnDisconnectedAsync(exception);
        }

        public void UpdateUser(string UserName)
        {
            var MachineID = Runtime.SharedObjects.GetMachineIDFromSessionID(Context.ConnectionId);
            if (!string.IsNullOrEmpty(MachineID))
            {
                var device = _context.Machines.Where(x => x.ID == MachineID).FirstOrDefault();
                if (device != null)
                {
                    device.LastHeardFrom = DateTime.Now;
                    device.CurrentUser = UserName;
                    _context.SaveChangesAsync();
                    var activity = new RecentActivity
                    {
                        type = ViewModels.RecentActivity.activityType.DeviceUserUpdate,
                        device = device.HostName,
                        result = true,
                    };
                    activity.GetActivityDescriptionByType();
                    activity.message += " User: " + UserName;
                    Runtime.SharedObjects.AddActivity(activity);
                }
            }
        }

        public void RegisterMachine(WOLMeshTypes.Models.DeviceIdentifier details)
        {

            var machine = _context.Machines.Where(x => x.ID == details.id).FirstOrDefault();
            if (machine == null)
            {
                machine = new DB.MachineItems
                {
                    ID = details.id,
                };
                _context.Machines.Add(machine);
                _context.SaveChangesAsync();
            }
            machine.HostName = details.HostName;
            if (!string.IsNullOrEmpty(details.CurrentUser))
            {
                machine.CurrentUser = details.CurrentUser;
            }
            machine.DomainName = details.DomainName;
            machine.LastHeardFrom = DateTime.Now;
            machine.WindowsVersion = details.WindowsVersion;
            machine.DeviceType = details.DeviceType;
            machine.LastWakeCount = 0;
            bool ChangesMadeToNetwork = false;

            foreach (var network in details.AccessibleNetworks)
            {
                var dbNetwork = _context.Networks.Where(x => x.SubnetMask == network.SubnetMask && x.BroadcastAddress == network.BroadcastAddress).FirstOrDefault();
                if (dbNetwork == null)
                {
                    ChangesMadeToNetwork = true;
                    DB.Networks newNet = new DB.Networks()
                    {
                        BroadcastAddress = network.BroadcastAddress,
                        SubnetMask = network.SubnetMask,

                    };

                    var activity = new ViewModels.RecentActivity
                    {
                        device = newNet.BroadcastAddress,
                        result = true,
                        type = ViewModels.RecentActivity.activityType.NetworkDiscovered
                    };
                    activity.GetActivityDescriptionByType();
                    Runtime.SharedObjects.AddActivity(activity);
                    _context.Networks.Add(newNet);
                }
            }

            if (details.AccessibleNetworks.Count == 1)
            {
                machine.BroadcastAddress = details.AccessibleNetworks[0].BroadcastAddress;
                machine.MacAddress = details.AccessibleNetworks[0].MacAddress;
                machine.IPAddress = details.AccessibleNetworks[0].IPAddress;

            }
            else if (details.AccessibleNetworks.Count == 0)
            {
                machine.BroadcastAddress = "None";
                machine.MacAddress = "Unknown";
            }
            else
            {
                machine.BroadcastAddress = "Multiple";
                machine.MacAddress = "Multiple";
            }
            if (ChangesMadeToNetwork)
            {
                _context.SaveChanges();
            }

            var oldMachineNetworks = _context.MachineNetworkDetails.Where(x => x.DeviceID == machine.ID);
            _context.MachineNetworkDetails.RemoveRange(oldMachineNetworks);

            foreach (var network in details.AccessibleNetworks)
            {
                _context.MachineNetworkDetails.Add(new DB.DeviceNetworkDetails
                {
                    DeviceID = machine.ID,
                    MacAddress = network.MacAddress,
                    NetworkID = _context.Networks.Where(x => x.SubnetMask == network.SubnetMask && x.BroadcastAddress == network.BroadcastAddress).First().NetworkID,
                    IPAddress = network.IPAddress

                });
            }
            _context.SaveChangesAsync();


            var accessibleNetworks = _context.MachineNetworkDetails.Where(x => x.DeviceID == machine.ID).Select(x => x.NetworkID).ToList();

            Runtime.SharedObjects.AddHubConnection(new ConnectionList.Connection
            {
                ConnectionID = Context.ConnectionId,
                ID = machine.ID,
                type = details.DeviceType.ToString(),
                AccessibleNetworks = accessibleNetworks,
                name = machine.HostName
            });


            try
            {

                Clients.Client(Context.ConnectionId).SendAsync("MachineSettings", new WOLMeshTypes.Models.MachineConfig
                {
                    HeartBeatIntervalSeconds = Runtime.SharedObjects.ServiceConfiguration.HeartBeatIntervalSeconds,
                    MaxPackets = Runtime.SharedObjects.ServiceConfiguration.PacketsToSend
                });

            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Failed to send MachineSettings to device {0}", machine.HostName);
            }
        }

        public void HeartBeat()
        {
            NLog.LogManager.GetCurrentClassLogger().Trace("HeartBeat received from {0}", Context.ConnectionId);
            var MachineID = Runtime.SharedObjects.GetMachineIDFromSessionID(Context.ConnectionId);
            if (!string.IsNullOrEmpty(MachineID))
            {

                var device = _context.Machines.Where(x => x.ID == MachineID).FirstOrDefault();
                NLog.LogManager.GetCurrentClassLogger().Trace("HeartBeat received from {0}. Updating Last Heard From", device.HostName);

                if (device != null)
                {
                    device.LastHeardFrom = DateTime.Now;
                    _context.SaveChangesAsync();
                }
            }
        }


    }
}
