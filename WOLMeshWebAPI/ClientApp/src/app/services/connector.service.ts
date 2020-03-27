import { Injectable, Inject, Type } from '@angular/core';

import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MachineItems, WakeUpCallResult, MachineDetailView, NetworkDetailView, ServiceSettings, RecentActivity } from '../classes/types';

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
  GetMachines(): Observable<MachineDetailView[]> {
    return this.http.get<MachineDetailView[]>('api/machines');
  }
  GetNetworks(): Observable<NetworkDetailView[]> {
    return this.http.get<NetworkDetailView[]>('api/networks');
  }
  GetServiceSettings(): Observable<ServiceSettings> {
    return this.http.get<ServiceSettings>('api/servicesettings');
  }
  SaveServiceSettings(settings: ServiceSettings): Observable<object> {
    return this.http.post<object>('api/servicesettings', settings);
  }
  DeleteNetwork(id: number): Observable<boolean> {
    return this.http.delete<boolean>('api/Networks/' + id);
  }
  DeleteMachine(id: string): Observable<boolean> {
    return this.http.delete<boolean>('api/machines/' + id);
  }

  WakeMachines(machines: string[]): Observable<WakeUpCallResult[]> {
    return this.http.post<WakeUpCallResult[]>('api/wakeup',machines);
  }

  GetActivity(): Observable<RecentActivity[]> {
    return this.http.get<RecentActivity[]>('api/activity');
  }
  ClearActivity(): Observable<object> {
    return this.http.delete<object>('api/activity');
  }
}
