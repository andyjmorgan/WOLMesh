export class MachineItems {
  public id: string;
  public hostName: string;
  public currentUser: string;
  public domainName: string;
  public windowsVersion: string;
  public ipAddress: string;
  public macAddress: string;
  public lastHeardFrom: Date;
  public isOnline: boolean;
}

export class WakeUpCallResult {
  public sent: boolean;
  public viaMachine: string;
  public machineName: string;
  public macAddress: string;
  public failureReason: string;
}

export class MachineDetailView {
  public machineSummary: MachineItems;
  public mappedNetworks: DeviceNetworkDetails[];
  public Online: boolean;
}

export class DeviceNetworkDetails {
  public DeviceID: string;
  public NetworkID: number;
  public MacAddress: string;
}
export class NetworkDetailView {
  public id: number;
  public broadcastAddress: string;
  public subnetMask: string;
  public registeredDevices: number;
  public onlineDevices: number;
}
export class ServiceSettings {
  public keepDevicesAwake: boolean;
  public includeWeekends: boolean;
  public startTime: number;
  public endTime: number;
  public heartBeatIntervalSeconds: number;
  public maxWakeRetries: number;
  public packetsToSend: number;
  public useDirectedBroadcasts: boolean;
  public maxStoredActivities: number;
}




export class RecentActivity {


  public time: Date;
  public type: string;
  public device: string;
  public message: string;
  public result: boolean;
  public errorReason: string;
}
