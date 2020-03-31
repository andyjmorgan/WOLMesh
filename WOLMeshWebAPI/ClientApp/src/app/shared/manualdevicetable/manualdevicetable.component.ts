import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { ManualMachineDetailView, WakeUpCallResult } from '../../classes/types';
import { ConnectorService } from '../../services/connector.service';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { ClrLoadingState } from '@clr/angular';

@Component({
  selector: 'app-manualdevicetable',
  templateUrl: './manualdevicetable.component.html',
  styleUrls: ['./manualdevicetable.component.css']
})
export class ManualdevicetableComponent implements OnInit {

  public wakeLoading: boolean = false;
  public selected: ManualMachineDetailView[] = [];
  public manualMachines: ManualMachineDetailView[]

  public showAddModal: boolean = false;
  public showDeleteModal: boolean = false;
  public showDeleteErrorOnline: boolean;
  public itemsToDelete: number[];

  public wakeRunning: boolean = false;
  public wakeUpResults: WakeUpCallResult[];

  public newMachineName: string = "";
  public newMachineMacAddress: string = "";
  public newMachineIPAddress: string = "";
  public newMachineBroadcastAddress: string = "";

  public discoveryFailureMessage: string = "";
  public discoveryFailed: boolean = false;

  public discoverybtnState: ClrLoadingState = ClrLoadingState.DEFAULT;
  public addbtnState: ClrLoadingState = ClrLoadingState.DEFAULT;


  @Output() UpdateAvailable: EventEmitter<number> = new EventEmitter();
  constructor(private formBuilder: FormBuilder, private connector: ConnectorService) { }


  clearDiscovery() {
    this.newMachineMacAddress = "";
    this.newMachineIPAddress = "";
    this.discoveryFailed = false;
    this.discoveryFailureMessage = "";
    this.newMachineBroadcastAddress = "";
  }

  public SettingsForm: FormGroup;

  ngOnInit() {


    this.SettingsForm = this.formBuilder.group({
      HostNameValidator: [null, [
        Validators.required,
      ]],
      MACAddressValidator: [null, [
        Validators.required,
        Validators.pattern(/^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$/)
      ]],
      IPAddressValidator: [null, [
        Validators.required,
        Validators.pattern(/^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$/)
      ]],
      BroadcastAddressValidator: [null, [
        Validators.required,
        Validators.pattern(/^(\d{1,3})\.(\d{1,3})\.(\d{1,3})\.(\d{1,3})$/)
      ]]

    })

    this.ReloadMachines();
  }

  ReloadMachines() {
    this.connector.GetManualMachines().subscribe(
      _machines => {
        this.manualMachines = _machines;
        console.log(_machines);
        this.UpdateAvailable.emit(this.manualMachines.length);
        //this.selected.length
      }
    );
  }
  WakeUp() {
    let idList: number[] = [];
    if (this.selected.length > 0) {
      this.wakeLoading = true;
      this.selected.forEach(item => {
        idList.push(item.id);
      });
      this.connector.WakeManualMachines(idList).subscribe(

        _results => {
          console.log("Wake Results: " + _results);
          this.wakeUpResults = _results;
          this.wakeLoading = false;
        });
    }
  }

  Delete() {
    this.itemsToDelete = [];
    if (this.selected.length > 0) {
      this.selected.forEach(item => {
          this.itemsToDelete.push(item.id);
        }
      )
    }

    if (this.itemsToDelete.length > 0 ) {
      this.showDeleteModal = true;
    }
    else {
      this.itemsToDelete = [];
    }
  }

  AddMachine() {
    this.addbtnState = ClrLoadingState.LOADING;
    let machine = new ManualMachineDetailView();
    machine.machineName = this.newMachineName;
    machine.broadcastAddress = this.newMachineBroadcastAddress;
    machine.lastKnownIP = this.newMachineIPAddress;
    machine.macAddress = this.newMachineMacAddress;

    this.connector.AddManualMachine(machine).subscribe(result => {
      console.log("Add Result: "+ result);
      if (result) {
        this.addbtnState = ClrLoadingState.SUCCESS;
        this.showAddModal = false;
        this.clearDiscovery();
        this.newMachineName = "";
        this.ReloadMachines();
      }
      else {
        this.addbtnState = ClrLoadingState.ERROR;
        this.discoveryFailed = true;
        this.discoveryFailureMessage = "Could not add device, is this a duplicate?";
      }
    })
  }

  ReallyDelete(result: boolean) {
    this.showDeleteModal = false;
    if (result) {

      this.itemsToDelete.forEach(item => {
        this.connector.DeleteManualMachine(item).subscribe(_result => {
          console.log("Delete Result = " + _result);
        });
      });
      this.ReloadMachines();
    }
  }

  Discover() {
    this.discoverybtnState = ClrLoadingState.LOADING;
    this.clearDiscovery();
    this.connector.DiscoverManualMachine(this.newMachineName).subscribe(result => {
      console.log(result);
      if (result.result) {
        this.discoverybtnState = ClrLoadingState.SUCCESS;

        this.newMachineMacAddress = result.macAddress;
        this.newMachineIPAddress = result.ipAddress;
      }
      else {
        this.discoverybtnState = ClrLoadingState.ERROR;

        this.discoveryFailed = true;
        this.discoveryFailureMessage = result.errorMessage;
      }
    });
  }

}
