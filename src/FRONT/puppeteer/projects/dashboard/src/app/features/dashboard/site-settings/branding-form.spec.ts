import { ComponentFixture, TestBed } from '@angular/core/testing';
import { BrandingFormComponent } from './branding-form';

describe('BrandingFormComponent', () => {
  let fixture: ComponentFixture<BrandingFormComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BrandingFormComponent],
    }).compileComponents();
  });

  function create(themeId: string) {
    fixture = TestBed.createComponent(BrandingFormComponent);
    fixture.componentRef.setInput('selectedThemeId', themeId);
    fixture.detectChanges();
  }

  it('hides hero fields for default theme', () => {
    create('default');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Conteúdo do hero');
    expect(el.textContent).not.toContain('Estatísticas');
  });

  it('shows hero and stats fields for minimal theme', () => {
    create('minimal');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Conteúdo do hero');
    expect(el.textContent).toContain('Estatísticas');
  });

  it('hides feature card fields for minimal theme', () => {
    create('minimal');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).not.toContain('Cards de destaque');
  });

  it('shows all sections for premium theme', () => {
    create('premium');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Conteúdo do hero');
    expect(el.textContent).toContain('Estatísticas');
    expect(el.textContent).toContain('Cards de destaque');
    expect(el.textContent).toContain('Imagem secundária');
    expect(el.textContent).toContain('CTA secundário');
    expect(el.textContent).toContain('WhatsApp');
  });

  it('shows WhatsApp for minimal theme', () => {
    create('minimal');
    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('WhatsApp');
  });

  it('emits draft changes', async () => {
    const spy = vi.fn();
    fixture = TestBed.createComponent(BrandingFormComponent);
    fixture.componentRef.setInput('selectedThemeId', 'default');
    fixture.componentInstance.draftChanged.subscribe(spy);
    fixture.detectChanges();
    await fixture.whenStable();
    expect(spy).toHaveBeenCalled();
    expect(spy.mock.calls[0][0]).toHaveProperty('primaryColor');
  });
});
