using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.Models
{
    public class ServiceSettings
    {
        public int DBVersion { get; set; }
        public int httpsPort { get; set; }
        public int relayCount { get; set; }
        public string SSLCertificatePFXFilePath { get; set; }
        public string SSLCertificatePFXPassword { get; set; }
        public int HeartBeatIntervalSeconds { get; set; }
        public bool KeepDevicesOnline { get; set; }
        public bool KeepDevicesOnlineAtWeekend { get; set; }
        public int KeepDevicesOnlineStartHour { get; set; }
        public int KeepDevicesOnlineEndHour { get; set; }
        public int MaxWakeRetries { get; set; }
        public bool UseDirectedBroadcasts { get; set; }
        public int MaxStoredActivities { get; set; }
        public int PacketsToSend { get; set; }

        public ServiceSettings()
        {
            DBVersion = 0;
            KeepDevicesOnline = false;
            KeepDevicesOnlineAtWeekend = false;
            KeepDevicesOnlineEndHour = 18;
            KeepDevicesOnlineStartHour = 8;
            httpsPort = 7443;
            relayCount = 3;
            HeartBeatIntervalSeconds = 120;
            MaxWakeRetries = 10;
            MaxStoredActivities = 200;
            PacketsToSend = 3;
            UseDirectedBroadcasts = true;
        }
    }
}
