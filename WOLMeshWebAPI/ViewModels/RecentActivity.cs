using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WOLMeshWebAPI.ViewModels
{

    public class RecentActivity
    {
        public enum activityType
        {
            WakeUpCall,
            PolicyWakeUpCall,
            UnknownDeviceWakeupCall,
            DeviceAdded,
            DeviceOnline,
            DeviceUpdate,
            DeviceUserUpdate,
            DeviceOffline,
            DeviceRemoved,
            NetworkRemoved,
            NetworkDiscovered,
            ExhaustedRetries,
            ServiceSettingsUpdate,
        }

        public void GetActivityDescriptionByType()
        {
            switch (this.type)
            {
                case activityType.DeviceOffline:
                    message=  "The Device went offline. ";
                    break;
                case activityType.DeviceOnline:
                    message=  "The Device came online. ";
                    break;
                case activityType.DeviceUpdate:
                    message = "The Device reported network changes. ";
                    break;
                case activityType.DeviceUserUpdate:
                    message = "The Devices user changed (logon / unlock). ";
                    break;
                case activityType.DeviceAdded:
                    message =  "A new registered device has been discovered. ";
                    break;
                case activityType.DeviceRemoved:
                    message =  "A network was removed. ";
                    break;
                case activityType.NetworkDiscovered:
                    message = "A new network has been discovered. ";
                    break;
                case activityType.NetworkRemoved:
                    message =  "A registered network was removed. ";
                    break;
                case activityType.WakeUpCall:
                    message =  "A wake up call was sent to the device. ";
                    break;
                case activityType.PolicyWakeUpCall:
                    message =  "The power policy sent a wake up call to the device. ";
                    break;
                case activityType.UnknownDeviceWakeupCall:
                    message =  "A wide scope wakeup was sent to an unregistered device. ";
                    break;
                case activityType.ExhaustedRetries:
                    message =  "Max number of retries reached for this device. No further wakeups will be sent. ";
                    break;
                case activityType.ServiceSettingsUpdate:
                    message= "The Web Service Settings have been updated.";
                    break;
                default:
                    message=  "Wait what? how did i end up here";
                    break;
            }
        }
        public DateTime time { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public activityType type { get; set; }
        public string device { get; set; }
        public string message { get; set; }
        public bool result { get; set; }
        public string errorReason { get; set; }

        public RecentActivity()
        {
            time = DateTime.Now;
        }
    }
}
