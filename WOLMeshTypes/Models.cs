﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WOLMeshTypes
{

    public class Models
    {
        public const string MachineType = "Machine";
        public const string HostType = "Host";


        public class NodeConfig
        {
            public string serveraddress { get; set; }
            public bool ignoreSSLErrors { get; set; }
            public int timerInterval { get; set; }
            public NodeConfig()
            {
                timerInterval = 30;
                ignoreSSLErrors = true;
                serveraddress = "https://serverhost.domain.local:7443";
            }

        }
        public class DeviceIdentifier
        {
            public string HostName { get; set; }
            public string DomainName { get; set; }
            public string WindowsVersion { get; set; }
            public bool IsNetworkAvailable { get; set; }
            public string CurrentUser { get; set; }
            public List<NetworkDetails> AccessibleNetworks { get; set; }
            public string id { get; set; }

            public DeviceIdentifier()
            {
                AccessibleNetworks = new List<NetworkDetails>();
            }
        }
        public class NetworkDetails
        {
            public string IPAddress { get; set; }
            public string SubnetMask { get; set; }
            public string BroadcastAddress { get; set; }
            public string MacAddress { get; set; }

        }

        public class WakeUpCall
        {
            public string MacAddress { get; set; }
            public string BroadcastAddress { get; set; }
            public string SubnetMask { get; set; }

        }

        public class WakeUpCallResult
        {
            public bool Sent { get; set; }
            public string ViaMachine { get; set; }
            public string MachineName { get; set; }
            public string MacAddress { get; set; }
            public string FailureReason { get; set; }
        }

        public class MachineDetails
        {
            public class MachineItems
            {
                public string ID { get; set; }
                public string HostName { get; set; }
                public string CurrentUser { get; set; }
                public string DomainName { get; set; }
                public string WindowsVersion { get; set; }
                public DateTime LastHeardFrom { get; set; }
                public string ipAddress { get; set; }
                public string macAddress { get; set; }
                public bool IsOnline { get; set; }
            }

            public class DeviceNetworkDetails
            {
                public int NetworkID { get; set; }
                public string MacAddress { get; set; }
                public string SubnetMask { get; set; }
                public string CurrentIP { get; set; }
                public string BroadcastAddress { get; set; }

            }

            public MachineItems machineSummary { get; set; }
            public List<DeviceNetworkDetails> MappedNetworks { get; set; }
            public MachineDetails()
            {
                MappedNetworks = new List<DeviceNetworkDetails>();
            }
        }

    }
}