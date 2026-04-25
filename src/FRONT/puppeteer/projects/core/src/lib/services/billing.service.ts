import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../http/api-client.service';
import {
  BillingOverview,
  BillingPortalSessionResponse,
  ChangePlanResponse,
} from '../models/billing.model';

@Injectable({ providedIn: 'root' })
export class BillingService {
  private readonly api = inject(ApiClientService);

  getOverview(): Observable<BillingOverview> {
    return this.api.get<BillingOverview>('/api/tenant/payments/overview');
  }

  createPortalSession(returnUrl: string): Observable<BillingPortalSessionResponse> {
    return this.api.post<BillingPortalSessionResponse>('/api/tenant/payments/portal-session', { returnUrl });
  }

  changePlan(planId: string, successUrl?: string, cancelUrl?: string): Observable<ChangePlanResponse> {
    return this.api.post<ChangePlanResponse>('/api/tenant/plan', {
      plan_id: planId,
      success_url: successUrl,
      cancel_url: cancelUrl,
    });
  }
}
