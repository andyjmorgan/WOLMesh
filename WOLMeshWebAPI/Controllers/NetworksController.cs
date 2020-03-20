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
    public class NetworksController : ControllerBase
    {
        // GET: api/Networks
        DB.AppDBContext _context;
        public NetworksController(DB.AppDBContext context)
        {
            _context = context;

        }

        [HttpGet]
        public List<DB.Networks> Get()
        {
            return _context.Networks.ToList();
        }


    }
}
