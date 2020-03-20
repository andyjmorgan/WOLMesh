import { Component, OnInit } from '@angular/core';
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
  constructor(private connector: ConnectorService) { }

  ngOnInit() {
    this.ReloadMachines();
  }

  ReloadMachines() {
    this.connector.GetMachines().subscribe(
      _machines => {
        this.machines = _machines;
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
      this.connector.WakeMachines(idList).subscribe(

        _results => {
          this.wakeUpResults = _results;
          this.wakeLoading = false;
        })
    }
  }
}
