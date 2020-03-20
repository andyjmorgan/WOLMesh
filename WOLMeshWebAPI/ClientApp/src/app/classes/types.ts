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
  public   NetworkID: number;
  public   MacAddress: string;
}
