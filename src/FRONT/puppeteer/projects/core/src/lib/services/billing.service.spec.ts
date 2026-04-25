import { describe, it, expect, beforeEach, afterEach } from 'vitest';
import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { BillingService } from './billing.service';
import { ApiClientService } from '../http/api-client.service';
import { environment } from '../environments/environment';

describe('BillingService', () => {
  let service: BillingService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [BillingService, ApiClientService, provideHttpClient(), provideHttpClientTesting()],
    });

    service = TestBed.inject(BillingService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('getOverview() calls GET /api/tenant/payments/overview', () => {
    service.getOverview().subscribe();

    const req = http.expectOne(`${environment.apiBaseUrl}/api/tenant/payments/overview`);
    expect(req.request.method).toBe('GET');

    req.flush({
      payment_status: 'paid',
      subscription_status: 'active',
      has_recurring_payment: true,
      can_manage_billing: true,
      recent_transactions: [],
    });
  });

  it('createPortalSession() calls POST /api/tenant/payments/portal-session', () => {
    service.createPortalSession('https://consultor.app/dashboard/plan').subscribe();

    const req = http.expectOne(`${environment.apiBaseUrl}/api/tenant/payments/portal-session`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ returnUrl: 'https://consultor.app/dashboard/plan' });

    req.flush({ url: 'https://billing.stripe.com/session/test' });
  });

  it('changePlan() posts the selected plan with absolute checkout callback URLs', () => {
    service.changePlan(
      'plan_123',
      'http://consultor.localhost/dashboard/plan?checkout=success',
      'http://consultor.localhost/dashboard/plan?checkout=cancel',
    ).subscribe();

    const req = http.expectOne(`${environment.apiBaseUrl}/api/tenant/plan`);
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      plan_id: 'plan_123',
      success_url: 'http://consultor.localhost/dashboard/plan?checkout=success',
      cancel_url: 'http://consultor.localhost/dashboard/plan?checkout=cancel',
    });

    req.flush({
      plan: {
        id: 'plan_123',
        name: 'Starter',
        description: 'Starter',
        price_per_month: 99,
        currency_code: 'BRL',
        max_properties: 25,
        video_upload: false,
        ai_descriptions: false,
        custom_domain: false,
        premium_analytics: false,
        is_active: true,
      },
      requires_checkout: true,
      checkout_url: 'https://checkout.stripe.test/session_123',
      updated_in_place: false,
      message: 'Checkout session created successfully.',
    });
  });
});
