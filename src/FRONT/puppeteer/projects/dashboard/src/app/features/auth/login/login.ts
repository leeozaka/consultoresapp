import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  OnInit,
  PLATFORM_ID,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '@consultores/core';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly http = inject(HttpClient);
  private readonly fb = inject(FormBuilder);
  private readonly platformId = inject(PLATFORM_ID);

  protected readonly loginForm = this.fb.nonNullable.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required, Validators.minLength(8)]],
  });

  protected readonly isSubmitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  private readonly sessionExpiredMessage = $localize`:@@auth.sessionExpired.detail:Você foi desconectado. Faça login novamente.`;
  private readonly pendingAccountMessage = $localize`:@@auth.accountPending.detail:Sua conta está pendente de aprovação. Um administrador precisa atribuí-la a um tenant antes de você poder entrar.`;

  private returnUrl = '';

  ngOnInit(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    this.returnUrl = this.route.snapshot.queryParamMap.get('returnUrl') ?? '';

    const urlError = this.route.snapshot.queryParamMap.get('error');
    if (urlError === 'account_pending') {
      this.errorMessage.set(this.pendingAccountMessage);
    } else if (this.route.snapshot.queryParamMap.get('sessionExpired') === '1') {
      this.errorMessage.set(this.sessionExpiredMessage);
    }
  }

  protected onLogin(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);

    const { email, password } = this.loginForm.getRawValue();

    this.http
      .post(`${environment.apiBaseUrl}/api/auth/login`, { email, password }, { withCredentials: true })
      .subscribe({
        next: () => {
          globalThis.location.href = this.resolveSafeReturnUrl();
        },
        error: (err) => {
          this.isSubmitting.set(false);
          if (err.status === 401) {
            this.errorMessage.set('E-mail ou senha inválidos.');
          } else if (err.status === 403) {
            this.errorMessage.set(this.pendingAccountMessage);
          } else {
            this.errorMessage.set('Erro ao conectar. Tente novamente.');
          }
        },
      });
  }

  private resolveSafeReturnUrl(): string {
    if (!this.returnUrl) return '/';

    if (this.returnUrl.startsWith('/') && !this.returnUrl.startsWith('//')) {
      return this.returnUrl;
    }

    try {
      const parsed = new URL(this.returnUrl, globalThis.location.origin);
      if (parsed.origin === globalThis.location.origin) {
        return parsed.pathname + parsed.search + parsed.hash;
      }

      const issuer = new URL(environment.oidc.issuer);
      if (parsed.origin === issuer.origin && parsed.pathname === '/connect/authorize') {
        return parsed.toString();
      }
    } catch {
      return '/';
    }

    return '/';
  }
}
