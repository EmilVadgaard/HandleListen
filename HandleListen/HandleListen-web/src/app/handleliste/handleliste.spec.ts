import { ComponentFixture, TestBed } from '@angular/core/testing';

import { Handleliste } from './handleliste';

describe('Handleliste', () => {
  let component: Handleliste;
  let fixture: ComponentFixture<Handleliste>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Handleliste],
    }).compileComponents();

    fixture = TestBed.createComponent(Handleliste);
    component = fixture.componentInstance;
    await fixture.whenStable();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
