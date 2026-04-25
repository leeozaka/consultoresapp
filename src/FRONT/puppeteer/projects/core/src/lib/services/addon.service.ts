import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiClientService } from '../http/api-client.service';
// import {
//   PerkCatalogItem,
//   TenantAddon,
//   EnableAddonRequest,
//   EnableAddonResponse,
// } from '../models/addon.model';

@Injectable({ providedIn: 'root' })
export class AddonService {
  // private readonly api = inject(ApiClientService);

  // getCatalog(): Observable<PerkCatalogItem[]> {
  //   return this.api.get<PerkCatalogItem[]>('/api/addons/catalog');
  // }

  // getAdminCatalog(): Observable<PerkCatalogItem[]> {
  //   return this.api.get<PerkCatalogItem[]>('/api/admin/addons/catalog');
  // }

  // createCatalogItem(body: {
  //   key: string; name: string; description: string;
  //   pricePerMonth: number; currencyCode?: string;
  // }): Observable<PerkCatalogItem> {
  //   return this.api.post<PerkCatalogItem>('/api/admin/addons/catalog', body);
  // }

  // updateCatalogItem(id: string, body: {
  //   name: string; description: string; pricePerMonth: number;
  // }): Observable<PerkCatalogItem> {
  //   return this.api.put<PerkCatalogItem>(`/api/admin/addons/catalog/${id}`, body);
  // }

  // deactivateCatalogItem(id: string): Observable<void> {
  //   return this.api.delete<void>(`/api/admin/addons/catalog/${id}`);
  // }

  // getTenantAddons(tenantId: string): Observable<TenantAddon[]> {
  //   return this.api.get<TenantAddon[]>(`/api/admin/tenants/${tenantId}/addons`);
  // }

  // enable(tenantId: string, body: EnableAddonRequest): Observable<EnableAddonResponse> {
  //   return this.api.post<EnableAddonResponse>(`/api/admin/tenants/${tenantId}/addons`, {
  //     perk_key: body.perkKey,
  //     success_url: body.successUrl,
  //     cancel_url: body.cancelUrl,
  //   });
  // }

  // disable(tenantId: string, perkKey: string): Observable<void> {
  //   return this.api.delete<void>(`/api/admin/tenants/${tenantId}/addons/${perkKey}`);
  // }

  // // ── TenantAdmin self-service ────────────────────────────────────────────────

  // getMyAddons(): Observable<TenantAddon[]> {
  //   return this.api.get<TenantAddon[]>('/api/tenant/addons');
  // }

  // enableMyAddon(perkKey: string, successUrl?: string, cancelUrl?: string): Observable<EnableAddonResponse> {
  //   return this.api.post<EnableAddonResponse>('/api/tenant/addons', {
  //     perk_key: perkKey,
  //     success_url: successUrl,
  //     cancel_url: cancelUrl,
  //   });
  // }

  // disableMyAddon(perkKey: string): Observable<void> {
  //   return this.api.delete<void>(`/api/tenant/addons/${perkKey}`);
  // }
}
