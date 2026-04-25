import { Injectable, inject, computed, Signal } from '@angular/core';
import { TenantContextService } from '../tenant/tenant-context.service';
@Injectable({ providedIn: 'root' })
export class EntitlementService {
  private readonly tenantCtx = inject(TenantContextService);

  /**
   * Returns a computed Signal<boolean> that resolves to true if the current
   * tenant has the given feature entitlement active.
   *
   * Accepts either a known PerkKey (preferred) or a raw string. When a
   * PerkKey is provided, all alias entitlement keys for that perk are
   * checked (e.g. "whatsapp_button" and legacy "whatsappButton").
   */
  // hasFeature(key: PerkKey | string): Signal<boolean> {
  //   const rawKey: string = typeof key === 'string' ? key : (key as string);
  //   const definition = PERK_DEFINITIONS[rawKey as PerkKey];
  //   const entitlementKeys: string[] = definition?.entitlementKeys ?? [rawKey];

  //   return computed(() => {
  //     const entitlements = this.tenantCtx.currentTenant()?.entitlements;
  //     if (!entitlements) return false;

  //     for (const k of entitlementKeys) {
  //       if (k in entitlements && Boolean(entitlements[k])) {
  //         return true;
  //       }
  //     }

  //     return false;
  //   });
  // }
}
