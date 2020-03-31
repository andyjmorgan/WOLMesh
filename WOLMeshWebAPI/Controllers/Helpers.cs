using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static WOLMeshTypes.Models;

namespace WOLMeshWebAPI.Controllers
{
    public class Helpers
    {
        public static ViewModels.ManualMachineDiscovery DiscoverManualMachine(string dnsName)
        {
            NLog.LogManager.GetCurrentClassLogger().Info("Performing DNS lookup on: {0}", dnsName);

            System.Net.IPAddress[] addresses = null;
            try
            {
                addresses = System.Net.Dns.GetHostAddresses(dnsName);
                NLog.LogManager.GetCurrentClassLogger().Debug("Performing DNS lookup returned {0} addresses.", addresses.Length);

            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Performing DNS lookup failed on dns resoltion: {0}", ex.ToString());

                return new ViewModels.ManualMachineDiscovery
                {
                    result = false,
                    errorMessage = "Could not perform a dns lookup, " + ex.ToString()
                };
            }
            if (addresses.Length == 0)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Performing DNS lookup failed on as no addresses were received from dns");

                return new ViewModels.ManualMachineDiscovery
                {
                    result = false,
                    errorMessage = "Could not perform a dns lookup, no results received"
                };
            }
            else if (addresses.Length > 1)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Performing DNS lookup failed on as too many addresses were received from dns");
                return new ViewModels.ManualMachineDiscovery
                {
                    result = false,
                    errorMessage = "Dns Lookup returned more than one result. "
                };
            }
            else
            {

                try
                {
                    if (ArpLookup.Arp.IsSupported)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Debug("Performing Arp lookup on: {0} ", addresses[0].ToString());
                        Task<System.Net.NetworkInformation.PhysicalAddress> result = ArpLookup.Arp.LookupAsync(addresses[0]);
                        result.Wait();
                        NLog.LogManager.GetCurrentClassLogger().Debug("Performing Arp lookup returned: {0} ", Newtonsoft.Json.JsonConvert.SerializeObject(result, Formatting.Indented));

                        if (result.IsCompletedSuccessfully)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Info("Performing Arp lookup completed successfully. DNS Name: {0} - IP: {1} - Mac: {2}", dnsName, addresses[0].ToString(), result.Result.ToString());


                            return new ViewModels.ManualMachineDiscovery
                            {
                                result = true,
                                ipAddress = addresses[0].ToString(),
                                macAddress = WOLMeshTypes.IPAddressExtensions.FormatMacAddress(result.Result)
                            };
                        }
                        else
                        {
                            NLog.LogManager.GetCurrentClassLogger().Error("Arp task failed: {0}", Newtonsoft.Json.JsonConvert.SerializeObject(result, Formatting.Indented));
                            return new ViewModels.ManualMachineDiscovery
                            {
                                result = false,
                                errorMessage = "Could not perform an arp lookup, the task failed",
                            };
                        }
                    }
                    else
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error("Could not perform Arp lookup on: {0} as arp lookup is not supported on this platform", addresses[0].ToString());

                        return new ViewModels.ManualMachineDiscovery
                        {
                            result = false,
                            errorMessage = "ARP lookup is not supported on this platform"
                        };
                    }
                }
                catch(Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error("Could not perform Arp lookup as the arp query threw an exception: {0}", ex.ToString());

                    return new ViewModels.ManualMachineDiscovery
                    {
                        result = false,
                        errorMessage = "Could not perform Arp lookup as the arp query threw an exception: "+ ex.ToString()
                    };
                }
            }
        }

        public static ViewModels.SummaryView GetSummaryView(DB.AppDBContext _context)
        {
            var events = Runtime.SharedObjects.GetActivity();
            return new ViewModels.SummaryView
            {
                recentEvents = events.Count,
                registeredMachines = _context.Machines.Count(),
                registeredNetworks = _context.Networks.Count(),
                recentEventsHasError = events.Where(x => !x.result).ToList().Count > 0,
            };

        }
        public static List<ViewModels.NetworkDetailView> GetNetworkDetailView(DB.AppDBContext _context) 
        {
            List<ViewModels.NetworkDetailView> lndv = new List<ViewModels.NetworkDetailView>();
            foreach(var network in _context.Networks)
            {
                ViewModels.NetworkDetailView ndv = new ViewModels.NetworkDetailView
                {
                    id = network.NetworkID,
                    broadcastAddress = network.BroadcastAddress,
                    subnetMask = network.SubnetMask,
                };
                ndv.onlineDevices = Runtime.SharedObjects.GetOnlineSessionsByNetwork(network.NetworkID);
                ndv.manualDevices = _context.ManualMachines.Where(x => x.broadCastAddress == network.BroadcastAddress && x.isOnline).Count();
                ndv.registeredDevices = _context.MachineNetworkDetails.Where(x => x.NetworkID == network.NetworkID).Count();
                lndv.Add(ndv);
            }
            return lndv;
        }


        public static List<WOLMeshTypes.Models.WakeUpCallResult> WakeMachine(DB.MachineItems machine, DB.AppDBContext _context, IHubContext<Hubs.WOLMeshHub> _hub, List<Hubs.ConnectionList.Connection> activeDevices)
        {
            NLog.LogManager.GetCurrentClassLogger().Debug("Attempting to wake up Device: {0}", JsonConvert.SerializeObject(machine, Formatting.Indented));
            List<WOLMeshTypes.Models.WakeUpCallResult> wucResult = new List<WOLMeshTypes.Models.WakeUpCallResult>();
            var networks = _context.MachineNetworkDetails.Where(x => x.DeviceID == machine.ID).ToList();
            if (networks.Count > 0)
            {
                foreach (var network in networks)
                {
                    NLog.LogManager.GetCurrentClassLogger().Trace("WakeMachine Using Network: {0}", JsonConvert.SerializeObject(network, Formatting.Indented));
                    var networkObject = _context.Networks.Where(x => x.NetworkID == network.NetworkID).FirstOrDefault();
                    if (networkObject != null)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Trace("WakeMachine Using NetworkObject: {0}", JsonConvert.SerializeObject(networkObject, Formatting.Indented));

                        //send server local wakeup if possible

                        var ServerLocalNetwork = Runtime.SharedObjects.localNetworks.Where(x => x.BroadcastAddress == networkObject.BroadcastAddress && x.SubnetMask == networkObject.SubnetMask).FirstOrDefault();
                        if (ServerLocalNetwork != null)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Trace("WakeMachine Using ServerLocalNetwork: {0}", JsonConvert.SerializeObject(ServerLocalNetwork, Formatting.Indented));

                            NLog.LogManager.GetCurrentClassLogger().Debug("Attempting to wake up Device: {0} using servers local network.", machine.HostName);

                            try
                            {
                                WOLMeshTypes.WOL.WakeOnLan(new WakeUpCall
                                {
                                    BroadcastAddress = networkObject.BroadcastAddress,
                                    MacAddress = network.MacAddress,
                                    SubnetMask = networkObject.SubnetMask,
                                }, Runtime.SharedObjects.localNetworks, Runtime.SharedObjects.ServiceConfiguration.PacketsToSend);
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
                                NLog.LogManager.GetCurrentClassLogger().Error("Failed to relay a wakeup to {0} from the server: {2}", machine.HostName, ex.ToString());
                            }
                        }

                        //send relay wakeup if possible

                        var relayHubs = activeDevices.Where(x => x.AccessibleNetworks.Contains(network.NetworkID)).ToList();
                        if (relayHubs.Count > 0)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Trace("WakeMachine Using relayhubs: {0}", JsonConvert.SerializeObject(relayHubs, Formatting.Indented));

                            NLog.LogManager.GetCurrentClassLogger().Debug("Found {0} relays to wake up Device: {1}. Will use up to: {2}", relayHubs.Count,
                                machine.HostName,
                                Runtime.SharedObjects.ServiceConfiguration.relayCount);

                            var relays = relayHubs.Take(Runtime.SharedObjects.ServiceConfiguration.relayCount).ToList();


                            //use the relays
                            foreach (var relayhub in relays)
                            {
                                NLog.LogManager.GetCurrentClassLogger().Trace("WakeMachine Using relayhub: {0}", JsonConvert.SerializeObject(relayhub, Formatting.Indented));

                                try
                                {
                                    NLog.LogManager.GetCurrentClassLogger().Debug("Attempting to wake up Device: {0} using relay: {1}.", machine.HostName, relayhub.name);

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
                                    NLog.LogManager.GetCurrentClassLogger().Error("Attempting to wake up Device: {0} using a relay failed with Exception: {1}", machine.HostName, ex.ToString());

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

                        if (Runtime.SharedObjects.ServiceConfiguration.UseDirectedBroadcasts)
                        {
                            try
                            {
                                NLog.LogManager.GetCurrentClassLogger().Debug("Attempting to wake up Device: {0} using a directed broadcast.", machine.HostName);

                                NetworkDetails lnd = new NetworkDetails
                                {
                                    BroadcastAddress = networkObject.BroadcastAddress,
                                    SubnetMask = networkObject.SubnetMask,
                                    MacAddress = network.MacAddress
                                };
                                WOLMeshTypes.WOL.SUbnetDirectedWakeOnLan(network.MacAddress, lnd, Runtime.SharedObjects.ServiceConfiguration.PacketsToSend);
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
                                NLog.LogManager.GetCurrentClassLogger().Error("Attempting to wake up Device: {0} using a directed broadcast failed with Exception: {1}", machine.HostName, ex.ToString());

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


                        if (wucResult.Count <= 0)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Error("Attempting to wake up Device: {0} failed as no relays or directly connected networks were available to relay the message.");

                            wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                            {
                                MachineName = machine.HostName,
                                Sent = false,
                                FailureReason = "No Matching Networks found for device"
                            });
                        }
                    }
                    else
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error("Attempting to wake up Device: {0} failed with Error: {1}", machine.HostName, "No Matching Networks found for device.");

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
                NLog.LogManager.GetCurrentClassLogger().Error("Attempting to wake up Device: {0} failed with Error: {1}", machine.HostName, "No Networks associated with device.");

                wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                {
                    MachineName = machine.HostName,
                    Sent = false,
                    FailureReason = "No Networks associated with device"
                });
            }

            
            return wucResult;
        }

        public static List<WOLMeshTypes.Models.WakeUpCallResult> WakeManualMachine(DB.ManualMachineItems machine, DB.AppDBContext _context, IHubContext<Hubs.WOLMeshHub> _hub, List<Hubs.ConnectionList.Connection> activeDevices)
        {
            NLog.LogManager.GetCurrentClassLogger().Debug("Attempting to wake up Device: {0}", JsonConvert.SerializeObject(machine, Formatting.Indented));
            List<WOLMeshTypes.Models.WakeUpCallResult> wucResult = new List<WOLMeshTypes.Models.WakeUpCallResult>();


            //using servers local network
            var ServerLocalNetwork = Runtime.SharedObjects.localNetworks.Where(x => x.BroadcastAddress == machine.broadCastAddress).FirstOrDefault();
            if (ServerLocalNetwork != null)
            {
                NLog.LogManager.GetCurrentClassLogger().Trace("WakeMachine Using ServerLocalNetwork: {0}", JsonConvert.SerializeObject(ServerLocalNetwork, Formatting.Indented));

                NLog.LogManager.GetCurrentClassLogger().Debug("Attempting to wake up Device: {0} using servers local network.", machine.MachineName);

                try
                {
                    WOLMeshTypes.WOL.WakeOnLan(new WakeUpCall
                    {
                        BroadcastAddress = machine.broadCastAddress,
                        MacAddress = machine.MacAddress,
                        SubnetMask = ServerLocalNetwork.SubnetMask
                    }, Runtime.SharedObjects.localNetworks, Runtime.SharedObjects.ServiceConfiguration.PacketsToSend);
                    wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        MachineName = machine.MachineName,
                        Sent = true,
                        MacAddress = machine.MacAddress,
                        ViaMachine = Environment.MachineName
                    });
                }

                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error("Failed to relay a wakeup to {0} from the server: {2}", machine.MachineName, ex.ToString());
                }
            }


            //checking for relay hubs
            foreach (var network in _context.Networks)
            {
                if(network.BroadcastAddress == machine.broadCastAddress)
                {
                    var relayHubs = activeDevices.Where(x => x.AccessibleNetworks.Contains(network.NetworkID)).ToList();
                    if (relayHubs.Count > 0)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Trace("WakeMachine Using relayhubs: {0}", JsonConvert.SerializeObject(relayHubs, Formatting.Indented));

                        NLog.LogManager.GetCurrentClassLogger().Debug("Found {0} relays to wake up Device: {1}. Will use up to: {2}", relayHubs.Count,
                            machine.MachineName,
                            Runtime.SharedObjects.ServiceConfiguration.relayCount);

                        var relays = relayHubs.Take(Runtime.SharedObjects.ServiceConfiguration.relayCount).ToList();


                        //use the relays
                        foreach (var relayhub in relays)
                        {
                            NLog.LogManager.GetCurrentClassLogger().Trace("WakeMachine Using relayhub: {0}", JsonConvert.SerializeObject(relayhub, Formatting.Indented));

                            try
                            {
                                NLog.LogManager.GetCurrentClassLogger().Debug("Attempting to wake up Device: {0} using relay: {1}.", machine.MachineName, relayhub.name);

                                _hub.Clients.Client(relayhub.ConnectionID).SendAsync("WakeUp", new WOLMeshTypes.Models.WakeUpCall
                                {
                                    BroadcastAddress = machine.broadCastAddress,
                                    MacAddress = machine.MacAddress,
                                    SubnetMask = network.SubnetMask,
                                });
                                wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                                {
                                    MachineName = machine.MachineName,
                                    Sent = true,
                                    MacAddress = machine.MacAddress,
                                    ViaMachine = relayhub.name
                                });
                            }
                            catch (Exception ex)
                            {
                                NLog.LogManager.GetCurrentClassLogger().Error("Attempting to wake up Device: {0} using a relay failed with Exception: {1}", machine.MachineName, ex.ToString());

                                wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                                {
                                    MachineName = machine.MachineName,
                                    Sent = false,
                                    MacAddress = machine.MacAddress,
                                    ViaMachine = relayhub.name,
                                    FailureReason = "An Exception occurred while sending message to mesh: " + ex.ToString()
                                });
                            }
                        }
                    }

                }
            }

            //using direct broadcast
            if (Runtime.SharedObjects.ServiceConfiguration.UseDirectedBroadcasts)
            {
                try
                {
                    NLog.LogManager.GetCurrentClassLogger().Debug("Attempting to wake up Device: {0} using a directed broadcast.", machine.MachineName);

                    NetworkDetails lnd = new NetworkDetails
                    {
                        BroadcastAddress = machine.broadCastAddress,
                        SubnetMask = "n/a",
                        MacAddress = machine.MacAddress
                    };
                    WOLMeshTypes.WOL.SUbnetDirectedWakeOnLan(machine.MacAddress, lnd, Runtime.SharedObjects.ServiceConfiguration.PacketsToSend);
                    wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        MachineName = machine.MachineName,
                        Sent = true,
                        MacAddress = machine.MacAddress,
                        ViaMachine = "Directed Broadcast (" + lnd.BroadcastAddress + ")"
                    });
                }
                catch (Exception ex)
                {
                    NLog.LogManager.GetCurrentClassLogger().Error("Attempting to wake up Device: {0} using a directed broadcast failed with Exception: {1}", machine.MachineName, ex.ToString());

                    wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        MachineName = machine.MachineName,
                        Sent = false,
                        MacAddress = machine.MacAddress,
                        ViaMachine = "Directed Broadcast",
                        FailureReason = ex.ToString()
                    });
                }
            }


            if (wucResult.Count <= 0)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("Attempting to wake up Device: {0} failed as no relays or directly connected networks were available to relay the message.");

                wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                {
                    MachineName = machine.MachineName,
                    Sent = false,
                    MacAddress = machine.MacAddress,
                    FailureReason = "No Matching Networks found for device"
                }); 
            }
            return wucResult;
        }

        public static List<WOLMeshTypes.Models.WakeUpCallResult> WakeUnknownMachine(DB.AppDBContext _context, IHubContext<Hubs.WOLMeshHub> _hub, List<Hubs.ConnectionList.Connection> activeDevices, string macaddress)
        {
            List<WOLMeshTypes.Models.WakeUpCallResult> wucResult = new List<WOLMeshTypes.Models.WakeUpCallResult>();
            var allnetworks = _context.Networks.ToList();
            foreach (var network in allnetworks)
            {
                NLog.LogManager.GetCurrentClassLogger().Trace("WakeUnknownMachine Using Network: {0}", JsonConvert.SerializeObject(network, Formatting.Indented));

                //send subnet directed broadcast

                if (Runtime.SharedObjects.ServiceConfiguration.UseDirectedBroadcasts)
                {
                    NLog.LogManager.GetCurrentClassLogger().Debug("Attempting to wake up unknown device by mac: {0} using directed broadcast.", macaddress);

                    try
                    {
                        NetworkDetails lnd = new NetworkDetails
                        {
                            BroadcastAddress = network.BroadcastAddress,
                            SubnetMask = network.SubnetMask,
                            MacAddress = macaddress
                        };
                        WOLMeshTypes.WOL.SUbnetDirectedWakeOnLan(macaddress, lnd, Runtime.SharedObjects.ServiceConfiguration.PacketsToSend);
                        wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                        {
                            MachineName = macaddress,
                            Sent = true,
                            MacAddress = macaddress,
                            ViaMachine = "Directed Broadcast (" + lnd.BroadcastAddress + ")"
                        });
                    }
                    catch (Exception ex)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error("Attempting to wake up unknown device by mac: {0} using directed broadcast failed with exception: {1}", macaddress, ex.ToString());

                        wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                        {
                            MachineName = macaddress,
                            Sent = false,
                            MacAddress = macaddress,
                            ViaMachine = "Directed Broadcast",
                            FailureReason = ex.ToString()
                        });
                    }
                }
               


                //send a wakeup from the server

                var localNetwork = Runtime.SharedObjects.localNetworks.Where(x => x.BroadcastAddress == network.BroadcastAddress && x.SubnetMask == network.SubnetMask).FirstOrDefault();
                if (localNetwork != null)
                {
                    try
                    {
                        NLog.LogManager.GetCurrentClassLogger().Trace("WakeUnknownMachine Using ServerLocalNetwork: {0}", JsonConvert.SerializeObject(localNetwork, Formatting.Indented));

                        WOLMeshTypes.WOL.WakeOnLan(new WakeUpCall
                        {
                            BroadcastAddress = network.BroadcastAddress,
                            MacAddress = macaddress,
                            SubnetMask = network.SubnetMask
                        }, Runtime.SharedObjects.localNetworks, Runtime.SharedObjects.ServiceConfiguration.PacketsToSend);
                        wucResult.Add(new WOLMeshTypes.Models.WakeUpCallResult
                        {
                            MachineName = macaddress,
                            Sent = true,
                            MacAddress = macaddress,
                            ViaMachine = Environment.MachineName
                        });
                    }
                    catch (Exception ex)
                    {
                        NLog.LogManager.GetCurrentClassLogger().Error("Failed to send an unknown wakeup to {0} from the server with exception: {1}", macaddress, ex.ToString());
                    }

                }

                //send wakeup from peer devices on this network

                var devices = activeDevices.Where(X => X.AccessibleNetworks.Contains(network.NetworkID));
                NLog.LogManager.GetCurrentClassLogger().Trace("WakeUnknownMachine Using relayhubs: {0}", JsonConvert.SerializeObject(devices, Formatting.Indented));
                if (devices.Count() > 0)
                {
                    foreach (var device in devices.Take(Runtime.SharedObjects.ServiceConfiguration.relayCount))
                    {
                        NLog.LogManager.GetCurrentClassLogger().Trace("WakeUnknownMachine Using relayhub: {0}", JsonConvert.SerializeObject(device, Formatting.Indented));

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
                            NLog.LogManager.GetCurrentClassLogger().Error("Failed to send an unknown wakeup to {0} from the relay {1} with exception: {2}", macaddress,device.name, ex.ToString());

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
                isRelay = machine.DeviceType == DeviceType.Relay,
                machineType = machine.DeviceType.ToString()
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
        public static List<WOLMeshTypes.Models.MachineDetails> GetMachinesDetailView(DB.AppDBContext _context)
        {
            List<WOLMeshTypes.Models.MachineDetails> returnList = new List<MachineDetails>();
            var machines = _context.Machines.ToList();
            foreach(DB.MachineItems machine in machines)
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
                    isRelay = (machine?.DeviceType ?? DeviceType.RegisteredMachine) == DeviceType.Relay
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
                returnList.Add(md);
            }
            return returnList;   
        }
    }
}
