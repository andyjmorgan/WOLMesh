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
    public class WakeManualMachineController : ControllerBase
    {


        IHubContext<Hubs.WOLMeshHub> _hub;

        DB.AppDBContext _context;


        public WakeManualMachineController(IHubContext<Hubs.WOLMeshHub> meshhub, DB.AppDBContext context)
        {
            _hub = meshhub;
            _context = context;

        }
        // GET: api/WakeManualMachine
        [HttpGet]
        public List<WOLMeshTypes.Models.WakeUpCallResult> Get()
        {
            List<WOLMeshTypes.Models.WakeUpCallResult> wcrList = new List<WOLMeshTypes.Models.WakeUpCallResult>();

            List<DB.ManualMachineItems> AllMachines = _context.ManualMachines.ToList();

            List<Hubs.ConnectionList.Connection> activeDevices = Runtime.SharedObjects.GetOnlineSessions();

            foreach(var machine in AllMachines)
            {
                var machineResults =  Controllers.Helpers.WakeManualMachine(machine, _context, _hub, activeDevices);

                var results = machineResults.Where(x => x.Sent).ToList();
                if (results.Count > 0)
                {
                    var activity = new ViewModels.RecentActivity
                    {
                        device = machine.MachineName,
                        result = true,
                        type = ViewModels.RecentActivity.activityType.ManualDeviceWakeupCall,
                    };
                    activity.GetActivityDescriptionByType();
                    Runtime.SharedObjects.AddActivity(activity);
                }
                else
                {
                    var activity = new ViewModels.RecentActivity
                    {
                        device = machine.MachineName,
                        result = false,
                        type = ViewModels.RecentActivity.activityType.ManualDeviceWakeupCall,
                        errorReason = "All Attempts made to wake device failed."
                    };
                    activity.GetActivityDescriptionByType();
                    Runtime.SharedObjects.AddActivity(activity);
                }

                wcrList.AddRange(machineResults);
            }




            return wcrList;
        }

        // GET: api/WakeManualMachine/5
        [HttpGet("{id}", Name = "WakeManualDevice")]
        public List<WOLMeshTypes.Models.WakeUpCallResult> Get(int id)
        {
            List<WOLMeshTypes.Models.WakeUpCallResult> wcrList = new List<WOLMeshTypes.Models.WakeUpCallResult>();
            List<Hubs.ConnectionList.Connection> activeDevices = Runtime.SharedObjects.GetOnlineSessions();

            DB.ManualMachineItems thisMachine = _context.ManualMachines.Where(x => x.id == id).FirstOrDefault();
            if(thisMachine!= null)
            {
                wcrList.AddRange(Controllers.Helpers.WakeManualMachine(thisMachine, _context, _hub, activeDevices));

                var results = wcrList.Where(x => x.Sent).ToList();
                if (results.Count > 0)
                {
                    var activity = new ViewModels.RecentActivity
                    {
                        device = thisMachine.MachineName,
                        result = true,
                        type = ViewModels.RecentActivity.activityType.ManualDeviceWakeupCall,
                    };
                    activity.GetActivityDescriptionByType();
                    Runtime.SharedObjects.AddActivity(activity);
                }
                else
                {
                    var activity = new ViewModels.RecentActivity
                    {
                        device = thisMachine.MachineName,
                        result = false,
                        type = ViewModels.RecentActivity.activityType.ManualDeviceWakeupCall,
                        errorReason = "All Attempts made to wake device failed."
                    };
                    activity.GetActivityDescriptionByType();
                    Runtime.SharedObjects.AddActivity(activity);
                }
            }
            else
            {
                wcrList.Add(new WOLMeshTypes.Models.WakeUpCallResult
                {
                    FailureReason = "Could not find machine details",
                    Sent = false,
                    MacAddress = "Unknown",
                    MachineName = "Unknown",
                });
            }

            return wcrList;

        }

        // POST: api/WakeManualMachine
        [HttpPost]
        public List<WOLMeshTypes.Models.WakeUpCallResult> Post([FromBody] List<int> value)
        {
            List<WOLMeshTypes.Models.WakeUpCallResult> wcrList = new List<WOLMeshTypes.Models.WakeUpCallResult>();
            List<Hubs.ConnectionList.Connection> activeDevices = Runtime.SharedObjects.GetOnlineSessions();
            foreach(var item in value)
            {
                DB.ManualMachineItems thisMachine = _context.ManualMachines.Where(x => x.id == item).FirstOrDefault();
                if (thisMachine != null)
                {
                    //wcrList.AddRange(Controllers.Helpers.WakeManualMachine(thisMachine, _context, _hub, activeDevices));
                    var machineResults = Controllers.Helpers.WakeManualMachine(thisMachine, _context, _hub, activeDevices);

                    var results = machineResults.Where(x => x.Sent).ToList();
                    if (results.Count > 0)
                    {
                        var activity = new ViewModels.RecentActivity
                        {
                            device = thisMachine.MachineName,
                            result = true,
                            type = ViewModels.RecentActivity.activityType.ManualDeviceWakeupCall,
                        };
                        activity.GetActivityDescriptionByType();
                        Runtime.SharedObjects.AddActivity(activity);
                    }
                    else
                    {
                        var activity = new ViewModels.RecentActivity
                        {
                            device = thisMachine.MachineName,
                            result = false,
                            type = ViewModels.RecentActivity.activityType.ManualDeviceWakeupCall,
                            errorReason = "All Attempts made to wake device failed."
                        };
                        activity.GetActivityDescriptionByType();
                        Runtime.SharedObjects.AddActivity(activity);
                    }
                    wcrList.AddRange(machineResults);
                }
                else
                {
                    wcrList.Add(new WOLMeshTypes.Models.WakeUpCallResult
                    {
                        FailureReason = "Could not find machine details for id: " + item,
                        Sent = false,
                        MacAddress = "Unknown",
                        MachineName = "Unknown",
                    });
                }
            }
            return wcrList;
        }
    }
}
