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
    public class ManualMachineController : ControllerBase
    {

        DB.AppDBContext _context;


        public ManualMachineController(DB.AppDBContext context)
        {
            _context = context;

        }
        // GET: api/ManualMachine
        [HttpGet]
        public List<ViewModels.ManualMachineDetailView> Get()
        {
            var machineList = new List<ViewModels.ManualMachineDetailView>();
            foreach(var machine in _context.ManualMachines)
            {
                machineList.Add(new ViewModels.ManualMachineDetailView
                {
                    id = machine.id,
                    lastKnownIP = machine.lastKnownIP,
                    MacAddress = machine.MacAddress,
                    MachineName = machine.MachineName,
                    broadcastAddress = machine.broadCastAddress,
                    isOnline = machine.isOnline,
                    lastHeardFrom = machine.LastHeardFrom
                });
            }
            return machineList;
        }

        // GET: api/ManualMachine/5
       

        // POST: api/ManualMachine
        [HttpPost]
        public bool Post([FromBody] ViewModels.ManualMachineDetailView value)
        {
            var duplicate = _context.ManualMachines.Where(x => x.MachineName.ToLower() == value.MachineName.ToLower()).FirstOrDefault();
            if(duplicate != null)
            {
                return false;
            }
            else
            {
                _context.ManualMachines.Add(new DB.ManualMachineItems
                {
                    broadCastAddress = value.broadcastAddress,
                    lastKnownIP = value.lastKnownIP,
                    MacAddress = value.MacAddress.Replace(":", "").Replace("-", ""),
                    MachineName = value.MachineName,
                    
                });
                _context.SaveChanges();
                return true;
            }  
        }

        // PUT: api/ManualMachine/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] ViewModels.ManualMachineDetailView value)
        {

        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public bool Delete(int id)
        {
            var item = _context.ManualMachines.Where(x => x.id == id).FirstOrDefault();
            if (item == null)
            {
                return false;
            }
            else
            {
                _context.ManualMachines.Remove(item);
                _context.SaveChanges();
                return true;
            }
        }

    }
}
