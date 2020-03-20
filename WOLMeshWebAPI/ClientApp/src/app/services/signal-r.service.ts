import { Injectable, EventEmitter } from '@angular/core';
import * as signalR from "@aspnet/signalr";

@Injectable({
  providedIn: 'root'
})
export class SignalRService {
  public messageReceived = new EventEmitter<string>();
  private hubConnection: signalR.HubConnection
  public data: string;
  public startConnection = () => {
    this.hubConnection = new signalR.HubConnectionBuilder()
      .withUrl('/wolmeshhub')
      .build();

    this.hubConnection
      .start()
      .then(() => {
        console.log('Connection started');
        this.receiveMessage();
      }
    )
      .catch(err => console.log('Error while starting connection: ' + err));

  }

  public sendHeartBeatMessage = () => {
    console.log(this.hubConnection.state);
    this.hubConnection.send("SendMessage","heartBeat", "testing123").catch(err => console.log('Error While sending ' + err));

  }

  public receiveMessage = () => {
    this.hubConnection.on("ReceiveMessage", function (user, message) {
      console.log("ReceiveMessage" + user + message);
      this.data = "ReceiveMessage" + user + message;
      this.messageReceived.emit(message);
    });


  }
}
