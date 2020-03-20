using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
                        var relayHubs = activeDevices.Where(x => x.AccessibleNetworks.Contains(network.NetworkID)).ToList();
                        if (relayHubs.Count > 0)
                        {
                            
                            foreach (var relayhub in relayHubs.Take(Runtime.SharedObjects.ServiceConfiguration.relayCount))
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
                                        ViaMachine = relayhub.name,
                                        FailureReason = "No Failure"
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
                        else
                        {
                            wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                            {
                                MachineName = machine.HostName,
                                Sent = false,
                                MacAddress = network.MacAddress,
                                ViaMachine = "none available",
                                FailureReason = "No relay devices associated with network: " + networkObject.BroadcastAddress
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
                                ViaMachine = device.name,
                                FailureReason = "No Failure"
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
                else
                {
                    wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        MachineName = macaddress,
                        Sent = false,
                        MacAddress = macaddress,
                        ViaMachine = "none available",
                        FailureReason = "Could not find a suitable device for network: " + network.BroadcastAddress
                    });
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
