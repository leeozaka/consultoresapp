import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiClientService } from '../http/api-client.service';
import { Plan } from '../models/tenant.model';
import { LIST_FETCH_PAGE_SIZE, PaginatedResponse } from '../models/pagination.model';
import { environment } from '../environments/environment';

export interface SignupRequest {
  agencyName: string;
  slug: string;
  contactEmail: string;
  contactPhone: string | null;
  planId: string;
  firstName: string;
  lastName: string;
  email: string;
  password: string;
  successUrl: string;
  cancelUrl: string;
}

export interface SignupResponse {
  tenantId: string;
  userId: string;
  checkoutUrl: string;
  dismissToken: string;
}

export interface SignupResumeResponse {
  tenantId: string;
  agencyName: string;
  slug: string;
  contactEmail: string;
  contactPhone: string | null;
  planId: string;
  ownerEmail: string;
  ownerFirstName: string;
  ownerLastName: string;
  onboardingState: string;
}

export interface SlugAvailabilityResponse {
  available: boolean;
  slug: string;
}

export interface OnboardingStatusResponse {
  tenantId: string;
  tenantStatus: string;
  paymentStatus: string;
  onboardingState: string;
  slug: string | null;
}

@Injectable({ providedIn: 'root' })
export class OnboardingService {
  private readonly api = inject(ApiClientService);

  signup(body: SignupRequest): Observable<SignupResponse> {
    return this.api.post<SignupResponse>('/api/onboarding/signup', body);
  }

  checkSlug(slug: string): Observable<SlugAvailabilityResponse> {
    return this.api.get<SlugAvailabilityResponse>(`/api/onboarding/check-slug/${slug}`);
  }

  getPlans(): Observable<Plan[]> {
    return this.api
      .get<PaginatedResponse<Plan>>('/api/onboarding/plans', { page: 1, pageSize: LIST_FETCH_PAGE_SIZE })
      .pipe(map((page) => page.items));
  }

  getStatus(tenantId: string): Observable<OnboardingStatusResponse> {
    return this.api.get<OnboardingStatusResponse>(`/api/onboarding/${tenantId}/status`);
  }

  getResume(tenantId: string): Observable<SignupResumeResponse> {
    return this.api.get<SignupResumeResponse>(`/api/onboarding/${tenantId}/resume`);
  }

  dismiss(tenantId: string, dismissToken: string): Observable<void> {
    return this.api.post<void>(`/api/onboarding/${tenantId}/dismiss`, { dismissToken });
  }

  statusEventsUrl(tenantId: string): string {
    return `${environment.apiBaseUrl}/api/onboarding/${tenantId}/status/events`;
  }
}
