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
        public List<ViewModels.NetworkDetailView> Get()
        {
            return Helpers.GetNetworkDetailView(_context);
        }

        [HttpDelete("{id}", Name = "DeleteNetwork")]
        public bool Delete(int id)
        {
            var currentNetworks = Helpers.GetNetworkDetailView(_context);
            var network = currentNetworks.Where(x => x.id == id).FirstOrDefault();
            if(network == null)
            {
                return false;
            }
            else
            {
                if(network.onlineDevices > 0)
                {
                    return false;
                }
                else
                {
                    var dbNetwork = _context.Networks.Where(x => x.NetworkID == id).FirstOrDefault();
                    if(dbNetwork == null)
                    {
                        return false;
                    }
                    else
                    {
                        var activity = new ViewModels.RecentActivity
                        {
                            device = dbNetwork.BroadcastAddress,
                            result = true,
                            type = ViewModels.RecentActivity.activityType.NetworkRemoved
                        };
                        activity.GetActivityDescriptionByType();
                        Runtime.SharedObjects.AddActivity(activity);
                        _context.Networks.Remove(dbNetwork);
                    }

                    var machineReferences = _context.MachineNetworkDetails.Where(x => x.NetworkID == id).ToList();
                    if(machineReferences.Count > 0)
                    {
                        _context.MachineNetworkDetails.RemoveRange(machineReferences);

                    }
                    _context.SaveChanges();
                    return true;
                }
            }
        }

    }
}
