import { Component, OnInit, EventEmitter, Output } from '@angular/core';
import { ConnectorService } from '../../services/connector.service';
import { DeviceNetworkDetails, NetworkDetailView } from '../../classes/types';

@Component({
  selector: 'app-networktable',
  templateUrl: './networktable.component.html',
  styleUrls: ['./networktable.component.css']
})
export class NetworktableComponent implements OnInit {

  public networks: NetworkDetailView[];
  public selected: NetworkDetailView[] = [];


  public showDeleteModal: boolean = false;
  public showDeleteErrorOnline: boolean;
  public itemsToDelete:number[];

  @Output() UpdateAvailable: EventEmitter<number> = new EventEmitter();

  constructor(private connector: ConnectorService) { }

  ngOnInit() {
    this.ReloadNetworks();
  }

  ReloadNetworks() {
    this.connector.GetNetworks().subscribe(
      _networks => {
        this.networks = _networks;
        this.UpdateAvailable.emit(this.networks.length);
      }
    );
  }

  Delete() {
    this.itemsToDelete = [];
    if (this.selected.length > 0) {
      this.selected.forEach(item => {
        if (item.onlineDevices > 0) {
          this.showDeleteErrorOnline = true;

        }
        this.itemsToDelete.push(item.id);
      })
    }

    if (this.itemsToDelete.length > 0 && !this.showDeleteErrorOnline) {
      this.showDeleteModal = true;
    }

  }

  ReallyDelete(result: boolean) {
    this.showDeleteModal = false;
    if (result) {

      this.itemsToDelete.forEach(item => {
        this.connector.DeleteNetwork(item).subscribe(_result => {
          console.log("Delete Result = " + _result);
        });

      });
      this.ReloadNetworks();
    }
  }
}
