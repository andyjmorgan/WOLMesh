using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.ViewModels
{
    public class MachineDetailView
    {
        public DB.MachineItems MachineSummary { get; set; }
        public List<DB.DeviceNetworkDetails> MappedNetworks { get; set; }
        public bool Online { get; set; }
    }
}
