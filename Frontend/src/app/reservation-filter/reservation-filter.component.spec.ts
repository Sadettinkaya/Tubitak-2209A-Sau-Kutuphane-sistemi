import { ComponentFixture, TestBed } from '@angular/core/testing';

import { ReservationFilterComponent  } from './reservation-filter.component';

describe('ReservationFilter', () => {
  let component: ReservationFilterComponent ;
  let fixture: ComponentFixture<ReservationFilterComponent >;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [ReservationFilterComponent ]
    })
    .compileComponents();

    fixture = TestBed.createComponent(ReservationFilterComponent );
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
