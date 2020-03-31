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
    public class WakeRegisteredMachineController : ControllerBase
    {

        IHubContext<Hubs.WOLMeshHub> _hub;

        DB.AppDBContext _context;


        public WakeRegisteredMachineController(IHubContext<Hubs.WOLMeshHub> meshhub, DB.AppDBContext context)
        {
            _hub = meshhub;
            _context = context;

        }
        // GET: api/WakeRegisteredMachine
        /// <summary>
        /// this will awake every registered machine
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public List<WOLMeshTypes.Models.WakeUpCallResult> Get()
        {

            List<WOLMeshTypes.Models.WakeUpCallResult> wcrList = new List<WOLMeshTypes.Models.WakeUpCallResult>();

            List<DB.MachineItems> AllMachines = _context.Machines.ToList();

            List<Hubs.ConnectionList.Connection> activeDevices = Runtime.SharedObjects.GetOnlineSessions();

            foreach (DB.MachineItems machine in AllMachines)
            {
                if (activeDevices.Where(x => x.ID == machine.ID).Count() > 0)
                {
                    wcrList.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        Sent = false,
                        FailureReason = "Device is already online",
                        MachineName = machine.HostName,
                    });
                    var activity = new ViewModels.RecentActivity
                    {
                        device = machine.HostName,
                        result = false,
                        type = ViewModels.RecentActivity.activityType.WakeUpCall,
                        errorReason = "Device is already online."
                    };
                }
                else
                {
                    wcrList.AddRange(Helpers.WakeMachine(machine, _context, _hub, activeDevices));

                    var results = wcrList.Where(x => x.Sent).ToList();
                    if (results.Count > 0)
                    {
                        var activity = new ViewModels.RecentActivity
                        {
                            device = machine.HostName,
                            result = true,
                            type = ViewModels.RecentActivity.activityType.WakeUpCall,
                        };
                        activity.GetActivityDescriptionByType();
                        Runtime.SharedObjects.AddActivity(activity);
                    }
                    else
                    {
                        var activity = new ViewModels.RecentActivity
                        {
                            device = machine.HostName,
                            result = false,
                            type = ViewModels.RecentActivity.activityType.WakeUpCall,
                            errorReason = "All Attempts made to wake device failed."
                        };
                        activity.GetActivityDescriptionByType();
                        Runtime.SharedObjects.AddActivity(activity);
                    }
                }


            }
            return wcrList;
        }

        // GET: api/WakeRegisteredMachine/5
        [HttpGet("{id}", Name = "WakeRegisteredDevice")]
        public List<WOLMeshTypes.Models.WakeUpCallResult> Get(string id)
        {
            List<WOLMeshTypes.Models.WakeUpCallResult> wucResults = new List<WOLMeshTypes.Models.WakeUpCallResult>();
            List<Hubs.ConnectionList.Connection> activeDevices = Runtime.SharedObjects.GetOnlineSessions();

            var machineDetails = _context.Machines.Where(x => x.ID == id).FirstOrDefault();
            if (machineDetails != null)
            {
                if (activeDevices.Where(x => x.ID == id).Count() <= 0)
                {
                    wucResults.AddRange(Helpers.WakeMachine(machineDetails, _context, _hub, activeDevices));
                 
                }
                else
                {
                    wucResults.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        FailureReason = "Machine is online",
                        Sent = false,
                        MacAddress = machineDetails.MacAddress,
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
                    MacAddress = "Unknown",
                    MachineName = "Unknown",
                });
    
            }

            var results = wucResults.Where(x => x.Sent).ToList();
            if (results.Count > 0)
            {
                var activity = new ViewModels.RecentActivity
                {
                    device = machineDetails.HostName,
                    result = true,
                    type = ViewModels.RecentActivity.activityType.WakeUpCall,
                };
                activity.GetActivityDescriptionByType();
                Runtime.SharedObjects.AddActivity(activity);
            }
            else
            {
                var activity = new ViewModels.RecentActivity
                {
                    device = machineDetails.HostName,
                    result = false,
                    type = ViewModels.RecentActivity.activityType.WakeUpCall,
                    errorReason = "All Attempts made to wake device failed."
                };
                activity.GetActivityDescriptionByType();
                Runtime.SharedObjects.AddActivity(activity);
            }
            return wucResults;
        }

        // POST: api/WakeRegisteredMachine
        // POST: api/WakeUp
        [HttpPost]
        public List<WOLMeshTypes.Models.WakeUpCallResult> Post([FromBody] List<string> ids)
        {
            List<WOLMeshTypes.Models.WakeUpCallResult> wcrList = new List<WOLMeshTypes.Models.WakeUpCallResult>();

            List<DB.MachineItems> AllMachines = _context.Machines.ToList();

            List<Hubs.ConnectionList.Connection> activeDevices = Runtime.SharedObjects.GetOnlineSessions();

            foreach (var id in ids)
            {
                List<WOLMeshTypes.Models.WakeUpCallResult> machineList = new List<WOLMeshTypes.Models.WakeUpCallResult>();

                var machine = _context.MachineNetworkDetails.Where(x => x.DeviceID.ToLower() == id).FirstOrDefault();
                if (machine != null)
                {
                    var machineDetails = _context.Machines.Where(x => x.ID == machine.DeviceID).FirstOrDefault();
                    if (machineDetails != null)
                    {
                        if (activeDevices.Where(x => x.ID == machine.DeviceID).Count() <= 0)
                        {
                            machineList.AddRange(Helpers.WakeMachine(machineDetails, _context, _hub, activeDevices));

                            
                        }
                        else
                        {
                            machineList.Add(new WOLMeshTypes.Models.WakeUpCallResult
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
                        machineList.Add(new WOLMeshTypes.Models.WakeUpCallResult
                        {
                            FailureReason = "Could not find machine details",
                            Sent = false,
                            MacAddress = machine.MacAddress,
                            MachineName = machineDetails.HostName,
                        });
                    }

                    var results = machineList.Where(x => x.Sent).ToList();
                    if (results.Count > 0)
                    {
                        var activity = new ViewModels.RecentActivity
                        {
                            device = machineDetails.HostName,
                            result = true,
                            type = ViewModels.RecentActivity.activityType.WakeUpCall,
                        };
                        activity.GetActivityDescriptionByType();
                        Runtime.SharedObjects.AddActivity(activity);
                    }
                    else
                    {
                        var activity = new ViewModels.RecentActivity
                        {
                            device = machineDetails.HostName,
                            result = false,
                            type = ViewModels.RecentActivity.activityType.WakeUpCall,
                            errorReason = "All Attempts made to wake device failed."
                        };
                        activity.GetActivityDescriptionByType();
                        Runtime.SharedObjects.AddActivity(activity);
                    }
                    wcrList.AddRange(machineList);

                }
                else
                {
                    //no machine details, we'll try to wake with a global call:
                    machineList.Add(new WOLMeshTypes.Models.WakeUpCallResult
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
