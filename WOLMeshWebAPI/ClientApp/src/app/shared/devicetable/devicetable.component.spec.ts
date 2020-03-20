import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { DevicetableComponent } from './devicetable.component';

describe('DevicetableComponent', () => {
  let component: DevicetableComponent;
  let fixture: ComponentFixture<DevicetableComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ DevicetableComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(DevicetableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
