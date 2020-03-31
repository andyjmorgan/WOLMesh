using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.ViewModels
{
    public class ManualMachineDiscovery
    {
        public bool result { get; set; }
        public string errorMessage { get; set; }
        public string ipAddress { get; set; }
        public string macAddress { get; set; }
    }
}
