import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { RecenteventsComponent } from './recentevents.component';

describe('RecenteventsComponent', () => {
  let component: RecenteventsComponent;
  let fixture: ComponentFixture<RecenteventsComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ RecenteventsComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(RecenteventsComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
