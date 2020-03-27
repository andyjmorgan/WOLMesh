import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { RecentActivity } from '../../classes/types';
import { ConnectorService } from '../../services/connector.service';

@Component({
  selector: 'app-recentevents',
  templateUrl: './recentevents.component.html',
  styleUrls: ['./recentevents.component.css']
})
export class RecenteventsComponent implements OnInit {
  public events: RecentActivity[];


  public showDeleteModal: boolean = false;
  public hasErrors: boolean = false;
  @Output() UpdateAvailable: EventEmitter<number> = new EventEmitter();
  constructor(private connector: ConnectorService) { }

  ngOnInit() {
    this.ReloadEvents();
  }

  ReloadEvents() {
    this.connector.GetActivity().subscribe(result => {
      this.events = result;
      this.hasErrors = false;
      this.events.forEach(value => {
        if (!value.result) {
          this.hasErrors = true;
        }
      });
    })
  }

  Delete() {  
      this.showDeleteModal = true;
  }

  ReallyDelete(result: boolean) {
    this.showDeleteModal = false;
    if (result) {

      this.connector.ClearActivity().subscribe(result => {
        console.log(result);
        this.ReloadEvents();
      })
    }
  }

}
