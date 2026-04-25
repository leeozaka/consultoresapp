import { ComponentFixture, TestBed } from '@angular/core/testing';
import { PortalPreviewComponent } from './portal-preview';

describe('PortalPreviewComponent', () => {
  let fixture: ComponentFixture<PortalPreviewComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PortalPreviewComponent],
    }).compileComponents();
  });

  it('renders container with correct styling', () => {
    fixture = TestBed.createComponent(PortalPreviewComponent);
    // Pass null baseTenant so the effect skips createComponent (no lazy load)
    fixture.componentRef.setInput('selectedThemeId', 'minimal');
    fixture.componentRef.setInput('baseTenant', null);
    fixture.detectChanges();

    const container = fixture.nativeElement.querySelector('div');
    expect(container).toBeTruthy();
    expect(container.style.height).toBe('500px');
  });

  it('creates component instance', () => {
    fixture = TestBed.createComponent(PortalPreviewComponent);
    fixture.componentRef.setInput('selectedThemeId', 'default');
    fixture.componentRef.setInput('baseTenant', null);
    fixture.detectChanges();
    expect(fixture.componentInstance).toBeTruthy();
  });
});
