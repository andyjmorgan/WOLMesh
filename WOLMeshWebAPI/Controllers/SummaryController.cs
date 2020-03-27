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
    public class SummaryController : ControllerBase
    {
        DB.AppDBContext _context;


        public SummaryController(DB.AppDBContext context)
        {
            _context = context;
        }
        // GET: api/Summary
        [HttpGet]
        public ViewModels.SummaryView Get()
        {
            return Controllers.Helpers.GetSummaryView(_context);
        }      
    }
}
