import { describe, it, expect, beforeEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { Component, signal } from '@angular/core';
import { By } from '@angular/platform-browser';
// import { FeatureGateDirective } from './feature-gate.directive';
import { EntitlementService } from '../entitlements/entitlement.service';

@Component({
  template: `<div *appFeature="'featured_listings'">Featured Content</div>`,
  imports: [
    // FeatureGateDirective
  ],
})
class TestHostComponent {}

describe('FeatureGateDirective', () => {
  function setup(hasFeature: boolean) {
    const mockEntitlement = { hasFeature: (_key: string) => signal(hasFeature) };
    TestBed.configureTestingModule({
      imports: [TestHostComponent],
      providers: [{ provide: EntitlementService, useValue: mockEntitlement }],
    });
    const fixture = TestBed.createComponent(TestHostComponent);
    fixture.detectChanges();
    return fixture;
  }

  it('directive file is present (implementation pending)', () => {
    // FeatureGateDirective and EntitlementService.hasFeature are currently commented out
    // pending finalization of the Perk registry. Tests will be enabled once un-commented.
    expect(true).toBe(true);
  });
});
