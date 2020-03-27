using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.ViewModels
{
    public class SummaryView
    {
        public int registeredMachines { get; set; }
        public int registeredNetworks { get; set; }
        public int recentEvents { get; set; }
        public bool recentEventsHasError { get; set; }
    }
}
