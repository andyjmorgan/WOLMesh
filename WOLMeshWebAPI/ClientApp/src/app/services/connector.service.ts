import { Injectable, Inject, Type } from '@angular/core';

import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MachineItems, WakeUpCallResult, MachineDetailView, NetworkDetailView, ServiceSettings, RecentActivity, ManualMachineDetailView, ManualMachineDiscovery } from '../classes/types';

@Injectable({
  providedIn: 'root'
})
export class ConnectorService {
  url: string;
  http: HttpClient;
  constructor(http: HttpClient, @Inject('BASE_URL') baseUrl: string) {
    this.url = baseUrl;
    this.http = http;
  }


  /// networks
  GetNetworks(): Observable<NetworkDetailView[]> {
    return this.http.get<NetworkDetailView[]>('api/networks');
  }
  DeleteNetwork(id: number): Observable<boolean> {
    return this.http.delete<boolean>('api/Networks/' + id);
  }



  /// service settings
  GetServiceSettings(): Observable<ServiceSettings> {
    return this.http.get<ServiceSettings>('api/servicesettings');
  }
  SaveServiceSettings(settings: ServiceSettings): Observable<object> {
    return this.http.post<object>('api/servicesettings', settings);
  }



  /// registered device calls
  GetMachines(): Observable<MachineDetailView[]> {
    return this.http.get<MachineDetailView[]>('api/machines');
  }
  DeleteMachine(id: string): Observable<boolean> {
    return this.http.delete<boolean>('api/machines/' + id);
  }
  WakeRegisteredMachines(machines: string[]): Observable<WakeUpCallResult[]> {
    return this.http.post<WakeUpCallResult[]>('api/wakeregisteredmachine', machines);
  }



  /// manual device calls
  GetManualMachines(): Observable<ManualMachineDetailView[]> {
    return this.http.get<ManualMachineDetailView[]>('api/manualmachine');
  }
  DeleteManualMachine(id: number): Observable<boolean> {
    return this.http.delete<boolean>('api/manualmachine/' + id);
  }
  AddManualMachine(machine: ManualMachineDetailView): Observable<object> {
    return this.http.post<ManualMachineDetailView>('api/manualmachine', machine);
  }
  WakeManualMachines(machines: number[]): Observable<WakeUpCallResult[]> {
    return this.http.post<WakeUpCallResult[]>('api/WakeManualMachine', machines);
  }
  DiscoverManualMachine(hostname: string): Observable<ManualMachineDiscovery> {
    return this.http.get<ManualMachineDiscovery>('api/discovermachine/' + hostname);
  }

  



  
  /// get Activity
  GetActivity(): Observable<RecentActivity[]> {
    return this.http.get<RecentActivity[]>('api/activity');
  }
  ClearActivity(): Observable<object> {
    return this.http.delete<object>('api/activity');
  }
}
