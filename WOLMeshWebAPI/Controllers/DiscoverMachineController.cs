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
    public class DiscoverMachineController : ControllerBase
    {
        // GET: api/DiscoverMachine

        // GET: api/DiscoverMachine/5
        [HttpGet("{hostname}", Name = "GetMacAddressViaHostName")]
        public ViewModels.ManualMachineDiscovery Get(string hostname)
        {
            return Controllers.Helpers.DiscoverManualMachine(hostname);
        }   
    }
}
