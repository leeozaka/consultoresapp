import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { map } from 'rxjs/operators';
import { ApiClientService } from '../http/api-client.service';
import { Plan } from '../models/tenant.model';
import { ChangePlanResponse } from '../models/billing.model';
import { LIST_FETCH_PAGE_SIZE, PaginatedResponse } from '../models/pagination.model';

export interface CreatePlanRequest {
  name: string;
  description: string;
  pricePerMonth: number;
  maxProperties: number;
  videoUpload: boolean;
  aiDescriptions: boolean;
  customDomain: boolean;
  premiumAnalytics: boolean;
  portalTheme: string;
}

export type UpdatePlanRequest = CreatePlanRequest;

@Injectable({ providedIn: 'root' })
export class PlanService {
  private readonly api = inject(ApiClientService);

  getPlans(): Observable<Plan[]> {
    return this.api
      .get<PaginatedResponse<Plan>>('/api/plans', { page: 1, pageSize: LIST_FETCH_PAGE_SIZE })
      .pipe(map((page) => page.items));
  }

  getAllPlans(): Observable<Plan[]> {
    return this.api
      .get<PaginatedResponse<Plan>>('/api/admin/plans', { page: 1, pageSize: LIST_FETCH_PAGE_SIZE })
      .pipe(map((page) => page.items));
  }

  createPlan(body: CreatePlanRequest): Observable<Plan> {
    return this.api.post<Plan>('/api/admin/plans', body);
  }

  updatePlan(id: string, body: UpdatePlanRequest): Observable<Plan> {
    return this.api.put<Plan>(`/api/admin/plans/${id}`, body);
  }

  deactivatePlan(id: string): Observable<void> {
    return this.api.delete<void>(`/api/admin/plans/${id}`);
  }

  changePlan(planId: string, successUrl?: string, cancelUrl?: string): Observable<ChangePlanResponse> {
    return this.api.post<ChangePlanResponse>('/api/tenant/plan', {
      plan_id: planId,
      success_url: successUrl,
      cancel_url: cancelUrl,
    });
  }
}
