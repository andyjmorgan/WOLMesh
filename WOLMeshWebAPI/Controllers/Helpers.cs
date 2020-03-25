using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static WOLMeshTypes.Models;

namespace WOLMeshWebAPI.Controllers
{
    public class Helpers
    {

       
        public static List<WOLMeshTypes.Models.WakeUpCallResult> WakeMachine(DB.MachineItems machine, DB.AppDBContext _context, IHubContext<Hubs.WOLMeshHub> _hub, List<Hubs.ConnectionList.Connection> activeDevices)
        {
            List<WOLMeshTypes.Models.WakeUpCallResult> wucResult = new List<WOLMeshTypes.Models.WakeUpCallResult>();
            var networks = _context.MachineNetworkDetails.Where(x => x.DeviceID == machine.ID).ToList();
            if (networks.Count > 0)
            {
                foreach (var network in networks)
                {
                    var networkObject = _context.Networks.Where(x => x.NetworkID == network.NetworkID).FirstOrDefault();
                    if (networkObject != null)
                    {

                        //send server local wakeup if possible

                        var ServerLocalNetwork = Runtime.SharedObjects.localNetworks.Where(x => x.BroadcastAddress == networkObject.BroadcastAddress && x.SubnetMask == networkObject.SubnetMask).FirstOrDefault();
                        if (ServerLocalNetwork != null)
                        {
                            try
                            {
                                WOLMeshTypes.WOL.WakeOnLan(new WakeUpCall
                                {
                                    BroadcastAddress = networkObject.BroadcastAddress,
                                    MacAddress = network.MacAddress,
                                    SubnetMask = networkObject.SubnetMask
                                }, Runtime.SharedObjects.localNetworks);
                                wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                                {
                                    MachineName = machine.HostName,
                                    Sent = true,
                                    MacAddress = network.MacAddress,
                                    ViaMachine = Environment.MachineName
                                });
                            }

                            catch (Exception ex)
                            {
                                NLog.LogManager.GetCurrentClassLogger().Error("Failed to relay a wakeup to {0} from the server: {2}", network.MacAddress, ex.ToString());
                            }
                        }

                        //send relay wakeup if possible

                        var relayHubs = activeDevices.Where(x => x.AccessibleNetworks.Contains(network.NetworkID)).ToList();
                        if (relayHubs.Count > 0)
                        {
                            var relays = relayHubs.Take(Runtime.SharedObjects.ServiceConfiguration.relayCount).ToList();
                           

                            //use the relays
                            foreach (var relayhub in relays)
                            {
                                try
                                {
                                    _hub.Clients.Client(relayhub.ConnectionID).SendAsync("WakeUp", new WOLMeshTypes.Models.WakeUpCall
                                    {
                                        BroadcastAddress = networkObject.BroadcastAddress,
                                        MacAddress = network.MacAddress,
                                        SubnetMask = networkObject.SubnetMask,
                                    });
                                   wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                                    {
                                        MachineName = machine.HostName,
                                        Sent = true,
                                        MacAddress = network.MacAddress,
                                        ViaMachine = relayhub.name
                                    });
                                }
                                catch (Exception ex)
                                {
                                    wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                                    {
                                        MachineName = machine.HostName,
                                        Sent = false,
                                        MacAddress = network.MacAddress,
                                        ViaMachine = relayhub.name,
                                        FailureReason = "An Exception occurred while sending message to mesh: " + ex.ToString()
                                    });
                                }
                            }
                        }


                        //send directed broadcast

                        try
                        {
                            NetworkDetails lnd = new NetworkDetails
                            {
                                BroadcastAddress = networkObject.BroadcastAddress,
                                SubnetMask = networkObject.SubnetMask,
                                MacAddress = network.MacAddress
                            };
                            WOLMeshTypes.WOL.SUbnetDirectedWakeOnLan(network.MacAddress, lnd);
                            wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                            {
                                MachineName = machine.HostName,
                                Sent = true,
                                MacAddress = network.MacAddress,
                                ViaMachine = "Directed Broadcast (" + lnd.BroadcastAddress + ")"
                            });
                        }
                        catch (Exception ex)
                        {
                            wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                            {
                                MachineName = machine.HostName,
                                Sent = false,
                                MacAddress = machine.MacAddress,
                                ViaMachine = "Directed Broadcast",
                                FailureReason = ex.ToString()
                            });
                        }
                    }
                    else
                    {
                        
                        wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                        {
                            MachineName = machine.HostName,
                            Sent = false,
                            FailureReason = "No Matching Networks found for device"
                        });  
                    }
                }
            }
            else
            {
                
                wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                {
                    MachineName = machine.HostName,
                    Sent = false,
                    FailureReason = "No Networks associated with device"
                });
            }
            return wucResult;
        }

        public static List<WOLMeshTypes.Models.WakeUpCallResult> WakeUnknownMachine(DB.AppDBContext _context, IHubContext<Hubs.WOLMeshHub> _hub, List<Hubs.ConnectionList.Connection> activeDevices, string macaddress)
        {
            List<WOLMeshTypes.Models.WakeUpCallResult> wucResult = new List<WOLMeshTypes.Models.WakeUpCallResult>();
            var allnetworks = _context.Networks.ToList();
            foreach(var network in allnetworks)
            {

                //send subnet directed broadcast

                try
                {
                    NetworkDetails lnd = new NetworkDetails
                    {
                        BroadcastAddress = network.BroadcastAddress,
                        SubnetMask = network.SubnetMask,
                        MacAddress = macaddress
                    };
                    WOLMeshTypes.WOL.SUbnetDirectedWakeOnLan(macaddress, lnd);
                    wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        MachineName = macaddress,
                        Sent = true,
                        MacAddress = macaddress,
                        ViaMachine = "Directed Broadcast (" + lnd.BroadcastAddress + ")"
                    }) ;
                }
                catch(Exception ex)
                {
                    wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        MachineName = macaddress,
                        Sent = false,
                        MacAddress = macaddress,
                        ViaMachine = "Directed Broadcast",
                        FailureReason = ex.ToString()
                    }) ;
                }
               
                
                 //send a wakeup from the server

                var localNetwork = Runtime.SharedObjects.localNetworks.Where(x => x.BroadcastAddress == network.BroadcastAddress && x.SubnetMask == network.SubnetMask).FirstOrDefault();
                if (localNetwork != null)
                {
                    try
                    {
                        WOLMeshTypes.WOL.WakeOnLan(new WakeUpCall
                        {
                            BroadcastAddress = network.BroadcastAddress,
                            MacAddress = macaddress,
                            SubnetMask = network.SubnetMask
                        }, Runtime.SharedObjects.localNetworks);
                        wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                        {
                            MachineName = macaddress,
                            Sent = true,
                            MacAddress = macaddress,
                            ViaMachine = Environment.MachineName
                        });
                    }
                    catch(Exception ex)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error("Failed to relay a wakeup to {0} from the server: {2}",macaddress, ex.ToString());
                    }
                  
                }
               
                //send wakeup from peer devices on this network

                var devices = activeDevices.Where(X => X.AccessibleNetworks.Contains(network.NetworkID));
                if(devices.Count() > 0)
                {
                    foreach(var device in devices.Take(Runtime.SharedObjects.ServiceConfiguration.relayCount))
                    {                   
                        try
                        {
                            _hub.Clients.Client(device.ConnectionID).SendAsync("WakeUp", new WOLMeshTypes.Models.WakeUpCall
                            {
                                BroadcastAddress = network.BroadcastAddress,
                                MacAddress = macaddress,
                                SubnetMask = network.SubnetMask,
                            });
                            wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                            {
                                MachineName = macaddress,
                                Sent = true,
                                MacAddress = macaddress,
                                ViaMachine = device.name
                            });
                        }
                        catch (Exception ex)
                        {
                            wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                            {
                                MachineName = macaddress,
                                Sent = false,
                                MacAddress = macaddress,
                                ViaMachine = device.name,
                                FailureReason = "An Exception occurred while sending message to mesh: " + ex.ToString()
                            });
                        }
                    }
                }                       
            }
            return wucResult;
        }

        public static WOLMeshTypes.Models.MachineDetails GetMachineDetailView(DB.AppDBContext _context, DB.MachineItems machine)
        {

            WOLMeshTypes.Models.MachineDetails md = new WOLMeshTypes.Models.MachineDetails();
            md.machineSummary = new WOLMeshTypes.Models.MachineDetails.MachineItems
            {
                CurrentUser = machine.CurrentUser,
                DomainName = machine.DomainName,
                HostName = machine.HostName,
                ID = machine.ID,
                LastHeardFrom = machine.LastHeardFrom,
                WindowsVersion = machine.WindowsVersion,
                ipAddress = machine.IPAddress,
                macAddress = machine.MacAddress,
                IsOnline = Runtime.SharedObjects.isMachineOnline(machine.ID),
            };

            foreach (var network in _context.MachineNetworkDetails.Where(x => x.DeviceID == machine.ID))
            {
                var networkDetail = _context.Networks.Where(x => x.NetworkID == network.NetworkID).FirstOrDefault();

                if (networkDetail != null)
                {
                    md.MappedNetworks.Add(new WOLMeshTypes.Models.MachineDetails.DeviceNetworkDetails
                    {
                        MacAddress = network.MacAddress,
                        BroadcastAddress = networkDetail.BroadcastAddress ?? "unknown",
                        SubnetMask = networkDetail.SubnetMask ?? "unknown"
                    });
                }
            }
            return md;
        }
    }
}
