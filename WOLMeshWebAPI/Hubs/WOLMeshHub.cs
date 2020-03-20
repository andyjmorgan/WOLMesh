using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.Hubs
{
    public class WOLMeshHub : Hub
    {
        DB.AppDBContext _context;
        public WOLMeshHub(DB.AppDBContext context)
        {
            _context = context;
        }
        public async Task SendMessage(string user, string message)
        {

            await Clients.All.SendAsync("ReceiveMessage", user, message);
            var id = Context.ConnectionId;
            //Clients.

        }

        public override Task OnConnectedAsync()
        {

            return base.OnConnectedAsync();
        }


        public override Task OnDisconnectedAsync(Exception exception)
        {
            if (exception != null)
            {
                Console.WriteLine("Disconnect: {0}", exception.ToString());

            }
            //var RemoveList = Runtime.SharedObjects.connections.Connections.Where(x => x.ConnectionID == Context.ConnectionId).ToList();
            Runtime.SharedObjects.RemoveHubConnection(Context.ConnectionId);


            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendToType(string type, string message)
        {
            var connectionList = Runtime.SharedObjects.connections.Connections.Where(x => x.type == type).ToList();
            foreach (var connection in connectionList)
            {
                await Clients.Client(connection.ConnectionID).SendAsync("TypeMessage", "Test Message");
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
            machine.CurrentUser = details.CurrentUser;
            machine.DomainName = details.DomainName;
            machine.LastHeardFrom = DateTime.Now;
            machine.WindowsVersion = details.WindowsVersion;
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
                    _context.Networks.Add(newNet);
                }

            }

            if (details.AccessibleNetworks.Count == 1)
            {
                machine.BroadcastAddress = details.AccessibleNetworks[0].BroadcastAddress;
                machine.MacAddress = details.AccessibleNetworks[0].MacAddress;
                machine.IPAddress = details.AccessibleNetworks[0].IPAddress;
            }
            else if(details.AccessibleNetworks.Count == 0)
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
                type = WOLMeshTypes.Models.MachineType,
                AccessibleNetworks = accessibleNetworks,
                name = machine.HostName
            });
        }

        public void HeartBeat()
        {
            var MachineID = Runtime.SharedObjects.GetMachineIDFromSessionID(Context.ConnectionId);
            if (!string.IsNullOrEmpty(MachineID))
            {
                var device = _context.Machines.Where(x => x.ID == MachineID).FirstOrDefault();
                if(device != null)
                {
                    device.LastHeardFrom = DateTime.Now;
                    _context.SaveChangesAsync();
                }
            }
        }

        
    }
}
