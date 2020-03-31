using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.ViewModels
{
    public class ServiceSettings
    {
        public bool KeepDevicesAwake { get; set; }
        public bool KeepManualDevicesAwake { get; set; }
        public bool IncludeWeekends { get; set; }
        public int startTime { get; set; }
        public int endTime { get; set; }
        public int HeartBeatIntervalSeconds { get; set; }
        public int MaxWakeRetries { get; set; }
        public int PacketsToSend { get; set; }
        public bool UseDirectedBroadcasts { get; set; }
        public int MaxStoredActivities { get; set; }
    }
}
