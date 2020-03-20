using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WOLMeshWebAPI.Authentication;

namespace WOLMeshWebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class LogonController : ControllerBase
    {

        private readonly IAuthenticationService _authService;
        public LogonController(IAuthenticationService authService)
        {
            _authService = authService;
        }
        [HttpPost]
        public void Post([FromBody] Models.MachineLogonModel value)
        {
            _authService.Login(value.machineID, value.machineName);
        }
    }
}
