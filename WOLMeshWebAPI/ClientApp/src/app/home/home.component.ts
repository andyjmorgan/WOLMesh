import { Component, OnInit } from '@angular/core';
import { SignalRService } from '../services/signal-r.service';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
})
export class HomeComponent implements OnInit {

  constructor(public signalR: SignalRService) { }

  ngOnInit() {
    this.signalR.startConnection();
    
    //this.signalR.sendHeartBeatMessage();
  }
  sendMessage() {
    this.signalR.sendHeartBeatMessage();
  }


}
