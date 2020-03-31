using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.ViewModels
{
    public class ManualMachineDetailView
    {
        public int id { get; set; }
        public string MachineName { get; set; }
        public string MacAddress { get; set; }
        public string lastKnownIP { get; set; }
        public string broadcastAddress { get; set; }
        public bool isOnline { get; set; }
        public DateTime lastHeardFrom { get; set; }

    }
}
