using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace WOLMeshWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class WakeUpController : ControllerBase
    {
        IHubContext<Hubs.WOLMeshHub> _hub;

        DB.AppDBContext _context;


        public WakeUpController(IHubContext<Hubs.WOLMeshHub> meshhub, DB.AppDBContext context)
        {
            _hub = meshhub;
            _context = context;

        }

        // GET: api/WakeUp
        [HttpGet]
        public List<WOLMeshTypes.Models.WakeUpCallResult> Get()
        {

            List<WOLMeshTypes.Models.WakeUpCallResult> wcrList = new List<WOLMeshTypes.Models.WakeUpCallResult>();

            List<DB.MachineItems> AllMachines = _context.Machines.ToList();

            List<Hubs.ConnectionList.Connection> activeDevices = Runtime.SharedObjects.GetOnlineSessions();

            foreach (DB.MachineItems machine in AllMachines)
            {
                if(activeDevices.Where(x=> x.ID == machine.ID).Count() > 0)
                {
                    wcrList.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        Sent = false,
                        FailureReason = "Device is already online",
                        MachineName = machine.HostName,
                    });
                }
                else
                {
                   wcrList.AddRange(Helpers.WakeMachine(machine, _context, _hub, activeDevices));
                }

            }
            return wcrList;
        }

        // GET: api/WakeUp/5
        [HttpGet("{mac}", Name = "WakeDevice")]
        public List<WOLMeshTypes.Models.WakeUpCallResult> Get(string mac)
        {
            mac = mac.Replace(":", "").Replace("-", "");
            List<WOLMeshTypes.Models.WakeUpCallResult> wucResults = new List<WOLMeshTypes.Models.WakeUpCallResult>();
            var machine = _context.MachineNetworkDetails.Where(x => x.MacAddress.ToLower() == mac.ToLower()).FirstOrDefault();
            List<Hubs.ConnectionList.Connection> activeDevices = Runtime.SharedObjects.GetOnlineSessions();

           
            if (machine != null)
            {
                var machineDetails = _context.Machines.Where(x => x.ID == machine.DeviceID).FirstOrDefault();
                if (machineDetails != null)
                {
                    if (activeDevices.Where(x => x.ID == machine.DeviceID).Count() <= 0)
                    {
                       wucResults.AddRange(Helpers.WakeMachine(machineDetails, _context, _hub, activeDevices));
                    }
                    else
                    {
                        wucResults.Add(new WOLMeshTypes.Models.WakeUpCallResult
                        {
                            FailureReason = "Machine is online",
                            Sent = false,
                            MacAddress = machine.MacAddress,
                            MachineName = machineDetails.HostName,
                        });
                    }
                }
                else
                {
                    wucResults.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        FailureReason = "Could not find machine details",
                        Sent = false,
                        MacAddress = machine.MacAddress,
                        MachineName = machineDetails.HostName,
                    });
                }              
            }
            else
            {
                //no machine details, we'll try to wake with a global call:
                wucResults.AddRange(Helpers.WakeUnknownMachine(_context, _hub, activeDevices, mac));
            }
            return wucResults;
        }


        // POST: api/WakeUp
        [HttpPost]
        public List<WOLMeshTypes.Models.WakeUpCallResult> Post([FromBody] List<string> ids)
        {
            List<WOLMeshTypes.Models.WakeUpCallResult> wcrList = new List<WOLMeshTypes.Models.WakeUpCallResult>();

            List<DB.MachineItems> AllMachines = _context.Machines.ToList();

            List<Hubs.ConnectionList.Connection> activeDevices = Runtime.SharedObjects.GetOnlineSessions();

            foreach (var id in ids)
            {
                var machine = _context.MachineNetworkDetails.Where(x => x.DeviceID.ToLower() == id).FirstOrDefault();
                if (machine != null)
                {
                    var machineDetails = _context.Machines.Where(x => x.ID == machine.DeviceID).FirstOrDefault();
                    if (machineDetails != null)
                    {
                        if (activeDevices.Where(x => x.ID == machine.DeviceID).Count() <= 0)
                        {
                            wcrList.AddRange(Helpers.WakeMachine(machineDetails, _context, _hub, activeDevices));
                        }
                        else
                        {
                            wcrList.Add(new WOLMeshTypes.Models.WakeUpCallResult
                            {
                                FailureReason = "Machine is online",
                                Sent = false,
                                MacAddress = machine.MacAddress,
                                MachineName = machineDetails.HostName,
                            });
                        }
                    }
                    else
                    {
                        wcrList.Add(new WOLMeshTypes.Models.WakeUpCallResult
                        {
                            FailureReason = "Could not find machine details",
                            Sent = false,
                            MacAddress = machine.MacAddress,
                            MachineName = machineDetails.HostName,
                        });
                    }
                }
                else
                {
                    //no machine details, we'll try to wake with a global call:
                    wcrList.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        FailureReason = "Could not find machine details",
                        Sent = false,
                        MacAddress = machine.DeviceID,
                    });
                }
            }
            return wcrList;
        }
    }
}
