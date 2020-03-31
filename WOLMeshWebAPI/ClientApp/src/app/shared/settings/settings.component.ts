import { Component, OnInit } from '@angular/core';
import { Options } from 'ng5-slider';
import { FormGroup, FormBuilder, Validators, ReactiveFormsModule  } from '@angular/forms';
import { ConnectorService } from '../../services/connector.service';
import { ServiceSettings } from '../../classes/types';

@Component({
  selector: 'app-settings',
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit {

  public sliderOptions: Options = {
    floor: 0,
    ceil: 23,
    showTicksValues: true,
    stepsArray: [
      { value: 0, legend: 'Midnight' },
      { value: 1 },
      { value: 2 },
      { value: 3 },
      { value: 4 },
      { value: 5 },
      { value: 6 },
      { value: 7 },
      { value: 8 },
      { value: 9, legend: '9:00 AM' },
      { value: 10 },
      { value: 11 },
      { value: 12 },
      { value: 13 },
      { value: 14 },
      { value: 15 },
      { value: 16 },
      { value: 17 },
      { value: 18, legend: '6:00 PM' },
      { value: 19 },
      { value: 20 },
      { value: 21 },
      { value: 22 },
      { value: 23 }
    ]
  }


  public SettingsForm: FormGroup;

  public settings: ServiceSettings;

  constructor(private formBuilder: FormBuilder, private connector: ConnectorService) { }

  ngOnInit() {

    this.SettingsForm = this.formBuilder.group({
      HeartBeatInterval: [null, [
        Validators.required,
        Validators.max(300),
        Validators.min(30)
      ]],
      AutoWakeUp: [null, [
        Validators.required
      ]],
      ManualAutoWakeUp: [null, [
        Validators.required
      ]],
      AutoWakeUpAtWeekend: [null, [
        Validators.required
      ]],
      MaxWakeRetries: [null, [
        Validators.required,
        Validators.min(3),
      Validators.max(30)
      ]],
      PacketsToSend: [null, [
        Validators.required,
        Validators.min(1),
        Validators.max(10)
      ]],
      UseDirectedBroadcasts: [null, [
        Validators.required,

      ]],
      MaxStoredActivities: [null, [
        Validators.required,
        Validators.min(0),
        Validators.max(500)
      ]],
     
    })

    this.connector.GetServiceSettings().subscribe(result => {
      this.settings = result;
    });

    
  }
  save() {
    this.connector.SaveServiceSettings(this.settings).subscribe(result => {
      console.log(result);
    }, error => {
      console.log("error");
    })
  }

}
