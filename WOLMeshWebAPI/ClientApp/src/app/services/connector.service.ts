import { Injectable, Inject, Type } from '@angular/core';

import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MachineItems, WakeUpCallResult, MachineDetailView } from '../classes/types';

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
  WakeMachines(machines: string[]): Observable<WakeUpCallResult[]> {
    return this.http.post<WakeUpCallResult[]>('api/wakeup',machines);
  }
}
