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
    public class ActivityController : ControllerBase
    {
        // GET: api/Activity
        [HttpGet]
        public List<ViewModels.RecentActivity> Get()
        {
            return Runtime.SharedObjects.GetActivity();
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete()]
        public IActionResult Delete()
        {
            Runtime.SharedObjects.ClearActivity();
            return Ok(true);
        }
    }
}
