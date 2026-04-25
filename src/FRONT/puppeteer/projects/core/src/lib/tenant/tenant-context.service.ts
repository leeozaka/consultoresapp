import { Injectable, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Observable, of, catchError } from 'rxjs';
import { tap } from 'rxjs/operators';
import { PortalLayoutMode, Tenant, TenantStatus } from '../models/tenant.model';
import { ApiClientService } from '../http/api-client.service';
import { BrandingApplierService } from './branding-applier.service';

@Injectable({ providedIn: 'root' })
export class TenantContextService {
  private readonly api = inject(ApiClientService);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly brandingApplier = inject(BrandingApplierService);

  private readonly _currentTenant = signal<Tenant | null>(null);
  private readonly _isLoading = signal(false);
  private readonly _tenantAccessBlockedReason = signal<string | null>(null);

  readonly currentTenant = this._currentTenant.asReadonly();
  readonly isLoading = this._isLoading.asReadonly();
  readonly tenantAccessBlockedReason = this._tenantAccessBlockedReason.asReadonly();
  readonly isTenantAccessBlocked = computed<boolean>(
    () => this._tenantAccessBlockedReason() !== null,
  );

  /**
   * Resolves the current tenant's portal routing mode from the API value.
   * 'Custom' is returned when the tenant has a non-empty `frontendOrigin` —
   * the public portal lazy-loads the sub-app registered for this tenant's slug
   * (see `custom-frontend.manifest.json` → generated `CUSTOM_PROJECT_LOADERS`).
   * Absolute `frontendOrigin` URLs are also used for OIDC allowlisting; Ingress
   * still serves the shared platform `frontend` Service. Perk changes without
   * coordinated UI updates can surface 402 responses in custom apps.
   */
  readonly portalLayoutMode = computed<PortalLayoutMode>(() =>
    this._currentTenant()?.portalLayoutMode ?? 'Default'
  );

  readonly isCustomFrontend = computed<boolean>(
    () => this.portalLayoutMode() === 'Custom'
  );

  readonly isSystemTenant = computed<boolean>(
    () => this._currentTenant()?.type === 'System',
  );

  readonly isTenantAccessible = computed<boolean>(() => {
    const tenant = this._currentTenant();
    if (!tenant) {
      return false;
    }

    return tenant.status === TenantStatus.Active && this._tenantAccessBlockedReason() === null;
  });

  readonly isInactiveTenant = computed<boolean>(() => {
    const tenant = this._currentTenant();
    if (!tenant || tenant.type === 'System') {
      return false;
    }

    return !this.isTenantAccessible();
  });

  /**
   * Resolves tenant by the current request host.
   *
   * Runs on both server and browser:
   *  - Server: the ssrOriginInterceptor prepends the backend origin and
   *    forwards X-Forwarded-Host so the backend resolves the right tenant.
   *  - Browser: relative URL goes through the ingress which routes by Host.
   *
   * Branding (CSS custom properties) is applied only on the browser because
   * it manipulates the DOM.
   */
  init(): Observable<Tenant | null> {
    this._isLoading.set(true);
    return this.api.get<Tenant>('/api/tenants/resolve').pipe(
      tap((tenant) => {
        this._currentTenant.set(tenant);
        this._tenantAccessBlockedReason.set(null);
        if (isPlatformBrowser(this.platformId) && tenant) {
          this.brandingApplier.applyTenant(tenant);
        }
        this._isLoading.set(false);
      }),
      catchError(() => {
        this._currentTenant.set(null);
        this._tenantAccessBlockedReason.set(null);
        this._isLoading.set(false);
        return of(null);
      })
    );
  }

  /**
   * Replaces the tenant context with the authenticated user's own tenant.
   * Must be called after auth completes so the Authorization header is present.
   * On the root domain the host-resolved tenant is always the landing tenant, so
   * the dashboard would otherwise read the wrong entitlements / plan.
   */
  refreshFromUser(): Observable<Tenant | null> {
    if (!isPlatformBrowser(this.platformId)) return of(null);

    return this.api.get<Tenant>('/api/tenants/me').pipe(
      tap((tenant) => {
        this._currentTenant.set(tenant);
        this._tenantAccessBlockedReason.set(null);
        if (tenant) {
          this.brandingApplier.applyTenant(tenant);
        }
      }),
      catchError(() => of(null))
    );
  }

  blockCurrentTenantAccess(reason?: string): void {
    this._tenantAccessBlockedReason.set(
      reason ?? $localize`:@@tenantUnavailable.blockedReason:O acesso deste tenant foi temporariamente bloqueado.`,
    );
  }
}
