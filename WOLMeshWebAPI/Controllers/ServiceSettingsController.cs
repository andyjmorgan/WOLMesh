using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WOLMeshWebAPI.ViewModels;

namespace WOLMeshWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceSettingsController : ControllerBase
    {
        // GET: api/ServiceSettings
        [HttpGet]
        public ServiceSettings Get()
        {
            var sc = Runtime.SharedObjects.ServiceConfiguration;

            return new ServiceSettings
            {
                KeepDevicesAwake = sc.KeepDevicesOnline,
                IncludeWeekends = Runtime.SharedObjects.ServiceConfiguration.KeepDevicesOnlineAtWeekend,
                startTime = Runtime.SharedObjects.ServiceConfiguration.KeepDevicesOnlineStartHour,
                endTime = Runtime.SharedObjects.ServiceConfiguration.KeepDevicesOnlineEndHour,
                HeartBeatIntervalSeconds = Runtime.SharedObjects.ServiceConfiguration.HeartBeatIntervalSeconds,
                UseDirectedBroadcasts = sc.UseDirectedBroadcasts,
                PacketsToSend = sc.PacketsToSend,
                MaxWakeRetries = sc.MaxWakeRetries,
                MaxStoredActivities = sc.MaxStoredActivities,
                KeepManualDevicesAwake = sc.KeepManualDevicesOnline
            };
        }


        // POST: api/ServiceSettings
        [HttpPost]
        public IActionResult Post([FromBody] ServiceSettings value)
        {
            
            Runtime.SharedObjects.ServiceConfiguration.HeartBeatIntervalSeconds = value.HeartBeatIntervalSeconds;
            Runtime.SharedObjects.ServiceConfiguration.KeepDevicesOnline = value.KeepDevicesAwake;
            Runtime.SharedObjects.ServiceConfiguration.KeepDevicesOnlineAtWeekend = value.IncludeWeekends;
            Runtime.SharedObjects.ServiceConfiguration.KeepDevicesOnlineStartHour = value.startTime;
            Runtime.SharedObjects.ServiceConfiguration.KeepDevicesOnlineEndHour = value.endTime;

            Runtime.SharedObjects.ServiceConfiguration.UseDirectedBroadcasts = value.UseDirectedBroadcasts;
            Runtime.SharedObjects.ServiceConfiguration.PacketsToSend = value.PacketsToSend;
            Runtime.SharedObjects.ServiceConfiguration.MaxWakeRetries = value.MaxWakeRetries;
            Runtime.SharedObjects.ServiceConfiguration.MaxStoredActivities = value.MaxStoredActivities;
            Runtime.SharedObjects.ServiceConfiguration.KeepManualDevicesOnline = value.KeepManualDevicesAwake;


            Runtime.SharedObjects.SaveConfig();
           
            return Ok(value);

        }

    }
}
