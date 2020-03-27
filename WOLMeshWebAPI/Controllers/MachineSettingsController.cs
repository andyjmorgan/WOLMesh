using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace WOLMeshWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class MachineSettingsController : ControllerBase
    {
        // GET: api/MachineSettings
        [HttpGet]
        public int Get()
        {
            return Runtime.SharedObjects.ServiceConfiguration.HeartBeatIntervalSeconds;
        }

    }
}
