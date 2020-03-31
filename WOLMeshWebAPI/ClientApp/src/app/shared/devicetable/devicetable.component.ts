import { Component, OnInit, EventEmitter, Output } from '@angular/core';
import { ConnectorService } from '../../services/connector.service';
import { MachineItems, WakeUpCallResult, MachineDetailView } from '../../classes/types';

@Component({
  selector: 'app-devicetable',
  templateUrl: './devicetable.component.html',
  styleUrls: ['./devicetable.component.css']
})
export class DevicetableComponent implements OnInit {

  public wakeLoading: boolean = false;
  
  public selected: MachineDetailView[]=[];
  public machines: MachineDetailView[];

  public wakeRunning: boolean = false;
  public wakeUpResults: WakeUpCallResult[];


  public showDeleteModal: boolean = false;
  public showDeleteErrorOnline: boolean;
  public itemsToDelete: string[];

  @Output() UpdateAvailable: EventEmitter<number> = new EventEmitter();
  constructor(private connector: ConnectorService) { }

  ngOnInit() {
    this.ReloadMachines();
  }

  ReloadMachines() {
    this.connector.GetMachines().subscribe(
      _machines => {
        this.machines = _machines;
        this.UpdateAvailable.emit(this.machines.length);
        //this.selected.length
      }
    );
  }
  WakeUp() {
    let idList: string[] = [];
    if (this.selected.length > 0) {
      this.wakeLoading = true;
      this.selected.forEach(item => {
        idList.push(item.machineSummary.id);
      });
      this.connector.WakeRegisteredMachines(idList).subscribe(

        _results => {
          this.wakeUpResults = _results;
          this.wakeLoading = false;
        });
    }
  }

  Delete() {
    this.itemsToDelete = [];
    if (this.selected.length > 0) {
      this.selected.forEach(item => {
        if (item.Online) {
          this.showDeleteErrorOnline = true;
        }
        else {
          this.itemsToDelete.push(item.machineSummary.id);
        }
      })
    }

    if (this.itemsToDelete.length > 0 && !this.showDeleteErrorOnline) {
      this.showDeleteModal = true;
    }
    else {
      this.itemsToDelete = [];
    }

  }

  ReallyDelete(result: boolean) {
    this.showDeleteModal = false;
    if (result) {
      
      this.itemsToDelete.forEach(item => {
        this.connector.DeleteMachine(item).subscribe(_result => {
          console.log("Delete Result = " + _result);
        });

      });
      this.ReloadMachines();
    }
  }
}
