import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { ManualdevicetableComponent } from './manualdevicetable.component';

describe('ManualdevicetableComponent', () => {
  let component: ManualdevicetableComponent;
  let fixture: ComponentFixture<ManualdevicetableComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ ManualdevicetableComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(ManualdevicetableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
