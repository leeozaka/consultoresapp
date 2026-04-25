import { Injectable, inject, signal, computed, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { OAuthService, AuthConfig } from 'angular-oauth2-oidc';
import { CurrentUser, UserRole } from '../models/auth.model';
import { environment } from '../environments/environment';

const authConfig: AuthConfig = {
  issuer: environment.oidc.issuer,
  clientId: environment.oidc.clientId,
  redirectUri: environment.oidc.redirectUri,
  postLogoutRedirectUri: environment.oidc.postLogoutRedirectUri,
  scope: environment.oidc.scope,
  responseType: environment.oidc.responseType,
  useSilentRefresh: false,
  sessionChecksEnabled: false,
  showDebugInformation: false,
  requireHttps: environment.production,
  strictDiscoveryDocumentValidation: true,
  skipIssuerCheck: false,
  clearHashAfterLogin: true,
};

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oauth = inject(OAuthService);
  private readonly platformId = inject(PLATFORM_ID);

  private readonly _currentUser = signal<CurrentUser | null>(null);
  private readonly _accessToken = signal<string | null>(null);

  readonly currentUser = this._currentUser.asReadonly();
  readonly accessToken = this._accessToken.asReadonly();
  readonly isAuthenticated = computed(() => this._accessToken() !== null);
  readonly isSuperAdmin = computed(() => this.hasRole('SuperAdmin'));

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      this.oauth.configure(authConfig);
      this.oauth.events.subscribe(() => this.syncTokenState());
    }
  }

  async init(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;

    try {
      // On the callback page, only load discovery; let handleCallback() own
      // the code exchange so the authorization code isn't consumed twice.
      const onCallback = globalThis.location?.pathname?.includes('/auth/callback');

      if (onCallback) {
        await this.oauth.loadDiscoveryDocument();
      } else {
        await this.oauth.loadDiscoveryDocumentAndTryLogin();
      }

      this.syncTokenState();

      if (this.oauth.hasValidAccessToken()) {
        await this.loadUserInfo();
      }
    } catch (err) {
      console.warn('[AuthService] OIDC discovery / login failed — app will continue unauthenticated.', err);
    }
  }

  login(): void {
    if (!this.oauth.loginUrl) {
      this.oauth
        .loadDiscoveryDocument(`${environment.oidc.issuer}/.well-known/openid-configuration`)
        .then((e) => {
          if (!this.oauth.loginUrl) {
            console.error('[AuthService] Discovery loaded but loginUrl is still falsy:', this.oauth.loginUrl);
            return;
          }
          this.oauth.initCodeFlow();
        })
        .catch((err) => console.error('[AuthService] Discovery document load failed:', err));
    } else {
      this.oauth.initCodeFlow();
    }
  }

  async handleCallback(): Promise<void> {
    // Single atomic call: loads discovery if needed, then exchanges the
    // authorization code for tokens.
    await this.oauth.loadDiscoveryDocumentAndTryLogin();
    this.syncTokenState();
    if (this.oauth.hasValidAccessToken()) {
      await this.loadUserInfo();
    }
  }

  logout(): void {
    this.oauth.logOut();
    this._currentUser.set(null);
    this._accessToken.set(null);
  }

  /**
   * Clears local auth state without triggering an OIDC end-session redirect.
   * Use when the OIDC / API server may be unavailable (e.g. 503 responses).
   */
  localLogout(): void {
    this.oauth.logOut(true); // noRedirectToLogoutUrl → tokens cleared, no server round-trip
    this._currentUser.set(null);
    this._accessToken.set(null);
  }

  async silentRefresh(): Promise<void> {
    await this.oauth.silentRefresh();
    this.syncTokenState();
  }

  hasRole(role: UserRole | string): boolean {
    return this._currentUser()?.roles.includes(role) ?? false;
  }

  private syncTokenState(): void {
    const token = this.oauth.hasValidAccessToken()
      ? this.oauth.getAccessToken()
      : null;
    this._accessToken.set(token);
  }

  private async loadUserInfo(): Promise<void> {
    try {
      const claims = this.oauth.getIdentityClaims() as Record<string, unknown>;
      if (claims) {
        // OpenIddict emits the standard 'role' claim (singular).
        // It may be a string (single role) or string[] (multiple).
        const rawRoles = claims['role'];
        const roles: string[] = Array.isArray(rawRoles)
          ? (rawRoles as string[])
          : rawRoles
          ? [rawRoles as string]
          : [];

        this._currentUser.set({
          id: claims['sub'] as string,
          email: claims['email'] as string,
          firstName: (claims['given_name'] as string) ?? '',
          lastName: (claims['family_name'] as string) ?? '',
          tenantId: (claims['tenant_id'] as string) ?? null,
          roles,
        });
      }
    } catch {
      this._currentUser.set(null);
    }
  }
}
