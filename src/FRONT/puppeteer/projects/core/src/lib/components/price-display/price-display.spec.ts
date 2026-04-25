import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PriceDisplayComponent } from './price-display';

describe('PriceDisplayComponent', () => {
  let fixture: ComponentFixture<PriceDisplayComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PriceDisplayComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(PriceDisplayComponent);
  });

  it('falls back to BRL when currency code is invalid', () => {
    fixture.componentRef.setInput('price', 1500);
    fixture.componentRef.setInput('currency', 'INVALID');
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('R$');
  });

  it('does not throw when price is NaN', () => {
    fixture.componentRef.setInput('price', Number.NaN);
    fixture.componentRef.setInput('currency', 'BRL');

    expect(() => fixture.detectChanges()).not.toThrow();
  });
});
