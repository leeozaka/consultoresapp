import { ChangeDetectionStrategy, Component, inject, OnInit, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '@consultores/core';

@Component({
  selector: 'app-logout',
  template: `
    <div class="flex min-h-screen flex-col items-center justify-center gap-4"
         style="background: var(--landing-bg, #0f1629)">
      <i class="pi pi-sign-out text-4xl opacity-30" style="color: #cbd5e1"></i>
      <p class="text-sm opacity-60" style="color: #cbd5e1" i18n>Saindo...</p>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LogoutComponent implements OnInit {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    this.auth.logout();
    this.router.navigate(['/']);
  }
}
