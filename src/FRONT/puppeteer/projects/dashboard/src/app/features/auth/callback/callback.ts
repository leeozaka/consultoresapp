import { ChangeDetectionStrategy, Component, inject, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService, TenantContextService, LoadingSpinnerComponent } from '@consultores/core';

@Component({
  selector: 'app-callback',
  imports: [LoadingSpinnerComponent],
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-4"
         style="background: var(--landing-bg, #0f1629)">
      <app-loading-spinner size="lg" />
      <p class="text-sm opacity-60" style="color: #cbd5e1" i18n>Autenticando...</p>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CallbackComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly tenantContext = inject(TenantContextService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);

  async ngOnInit(): Promise<void> {
    if (!isPlatformBrowser(this.platformId)) return;

    try {
      await this.auth.handleCallback();

      if (this.auth.isAuthenticated()) {
        // Replace the host-resolved (landing) tenant context with the user's own tenant
        // so entitlements, plan, and perk checks reflect the correct tenant immediately.
        await new Promise<void>((resolve) => {
          this.tenantContext.refreshFromUser().subscribe({ complete: () => resolve() });
        });

        const destination = this.auth.isSuperAdmin() ? '/admin' : '/dashboard';
        await this.router.navigate([destination]);
      } else {
        console.warn('[Callback] Token exchange did not produce a valid token.');
        await this.router.navigate(['/']);
      }
    } catch (err) {
      console.error('[Callback] handleCallback error:', err);
      await this.router.navigate(['/']);
    }
  }
}
