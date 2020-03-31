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

  

        // GET: api/WakeUp/5
        [HttpGet("{mac}", Name = "WakeUnknownDevice")]
        public List<WOLMeshTypes.Models.WakeUpCallResult> Get(string id)
        {
            id = id.Replace(":", "").Replace("-", "");
            List<WOLMeshTypes.Models.WakeUpCallResult> wucResults = new List<WOLMeshTypes.Models.WakeUpCallResult>();
            //var machine = _context.MachineNetworkDetails.Where(x => x.MacAddress.ToLower() == id.ToLower()).FirstOrDefault();
            List<Hubs.ConnectionList.Connection> activeDevices = Runtime.SharedObjects.GetOnlineSessions();


            //no machine details, we'll try to wake with a global call:
            wucResults.AddRange(Helpers.WakeUnknownMachine(_context, _hub, activeDevices, id));
            
            var results = wucResults.Where(x => x.Sent).ToList();
            if (results.Count > 0)
            {
                var activity = new ViewModels.RecentActivity
                {
                    device = id,
                    result = true,
                    type = ViewModels.RecentActivity.activityType.UnknownDeviceWakeupCall,
                };
                activity.GetActivityDescriptionByType();
                Runtime.SharedObjects.AddActivity(activity);
            }
            else
            {
                var activity = new ViewModels.RecentActivity
                {
                    device = id,
                    result = false,
                    type = ViewModels.RecentActivity.activityType.UnknownDeviceWakeupCall,
                    errorReason = "All Attempts made to wake device failed."
                };
                activity.GetActivityDescriptionByType();
                Runtime.SharedObjects.AddActivity(activity);
            }
            return wucResults;
        }
    }
}
