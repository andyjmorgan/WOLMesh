using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.ViewModels
{
    public class NetworkDetailView
    {
        public int id { get; set; }
        public string broadcastAddress { get; set; }
        public string subnetMask { get; set; }
        public int registeredDevices { get; set; }
        public int onlineDevices { get; set; }
    }
}
