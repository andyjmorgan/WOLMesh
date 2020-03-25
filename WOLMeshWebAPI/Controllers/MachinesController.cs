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
    public class MachinesController : ControllerBase
    {
        DB.AppDBContext _context;


        public MachinesController(DB.AppDBContext context)
        {
            _context = context;

        }
        // GET: api/Machines
        [HttpGet]
        public List<WOLMeshTypes.Models.MachineDetails> Get()
        {
            var returnList = new List<WOLMeshTypes.Models.MachineDetails>();
            var machines = _context.Machines.ToList();
            foreach (var machine in machines)
            {

                returnList.Add(Helpers.GetMachineDetailView(_context, machine));
            }
            return returnList;
        }

        // GET: api/Machines/5
        [HttpGet("{id}", Name = "GetMachines")]
        public WOLMeshTypes.Models.MachineDetails Get(string id)
        {

            DB.MachineItems machine = _context.Machines.Where(x => x.HostName.ToLower() == id.ToLower()).FirstOrDefault();
            if (machine == null)
            {
                var machineByMac = _context.MachineNetworkDetails.Where(x => x.MacAddress.ToLower() == id.ToLower()).FirstOrDefault();
                if (machineByMac != null)
                {
                    machine = _context.Machines.Where(x => x.ID == machineByMac.DeviceID).FirstOrDefault();
                    return Helpers.GetMachineDetailView(_context, machine);
                }
                return null;
            }
            else
            {
                return Helpers.GetMachineDetailView(_context, machine);
            }
        }

       
        [HttpDelete("{id}", Name = "DeleteMachine")]
        public bool Delete(string id)
        {
            var removeList = _context.Machines.Where(x => x.ID == id).ToList();
            _context.Machines.RemoveRange(removeList);
            var networkDetailsRemoveList = _context.MachineNetworkDetails.Where(x => x.DeviceID == id).ToList();
            _context.MachineNetworkDetails.RemoveRange(networkDetailsRemoveList);
            _context.SaveChanges();
            return true;
        }
    }
}
