import { async, ComponentFixture, TestBed } from '@angular/core/testing';

import { NetworktableComponent } from './networktable.component';

describe('NetworktableComponent', () => {
  let component: NetworktableComponent;
  let fixture: ComponentFixture<NetworktableComponent>;

  beforeEach(async(() => {
    TestBed.configureTestingModule({
      declarations: [ NetworktableComponent ]
    })
    .compileComponents();
  }));

  beforeEach(() => {
    fixture = TestBed.createComponent(NetworktableComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
