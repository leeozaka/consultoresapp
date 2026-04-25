import {
  ChangeDetectionStrategy,
  Component,
  inject,
  signal,
  OnInit,
  OnDestroy,
  PLATFORM_ID,
} from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { OnboardingService, OnboardingStatusResponse } from '@consultores/core';

interface TimelineStep {
  key: string;
  label: string;
  description: string;
  icon: string;
  status: 'complete' | 'active' | 'pending';
}

@Component({
  selector: 'app-onboarding-status',
  imports: [],
  templateUrl: './onboarding-status.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OnboardingStatusComponent implements OnInit, OnDestroy {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly onboarding = inject(OnboardingService);
  private readonly platformId = inject(PLATFORM_ID);

  protected readonly status = signal<OnboardingStatusResponse | null>(null);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly isLoading = signal(true);

  private eventSource?: EventSource;
  private tenantId = '';

  ngOnInit(): void {
    this.tenantId = this.route.snapshot.paramMap.get('tenantId') ?? '';
    if (!this.tenantId) {
      this.router.navigate(['/']);
      return;
    }
    this.loadStatus();
  }

  ngOnDestroy(): void {
    this.eventSource?.close();
  }

  private loadStatus(): void {
    this.onboarding.getStatus(this.tenantId).subscribe({
      next: (s) => {
        this.status.set(s);
        this.isLoading.set(false);
        if (s.onboardingState !== 'active') {
          this.connectSse();
        }
      },
      error: (err: { status?: number }) => {
        this.isLoading.set(false);
        if (err.status === 404) {
          this.status.set({
            tenantId: this.tenantId,
            onboardingState: 'abandoned',
            tenantStatus: 'pending',
            paymentStatus: 'none',
            slug: null,
          });
        } else {
          this.errorMessage.set('Não foi possível carregar o status. Tente novamente.');
        }
      },
    });
  }

  private connectSse(): void {
    if (!isPlatformBrowser(this.platformId)) return;

    const url = this.onboarding.statusEventsUrl(this.tenantId);
    this.eventSource = new EventSource(url);

    this.eventSource.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data) as { status: string; message: string };
        const current = this.status();
        if (!current) return;

        if (data.status === 'provisioning') {
          this.status.set({ ...current, onboardingState: 'provisioning' });
        } else if (data.status === 'active') {
          this.status.set({ ...current, onboardingState: 'active', tenantStatus: 'active' });
          this.eventSource?.close();
        } else if (data.status === 'awaiting_approval') {
          this.status.set({ ...current, onboardingState: 'awaiting_approval' });
        } else if (data.status === 'abandoned') {
          this.status.set({ ...current, onboardingState: 'abandoned' });
          this.eventSource?.close();
        }
      } catch {
        // ignore parse errors
      }
    };

    this.eventSource.onerror = () => {
      // SSE will auto-reconnect; silently ignore transient errors
    };
  }

  protected get timelineSteps(): TimelineStep[] {
    const state = this.status()?.onboardingState ?? 'pending_payment';

    const ordered: TimelineStep[] = [
      {
        key: 'payment',
        label: 'Pagamento',
        description: 'Checkout via Stripe concluído',
        icon: 'pi pi-credit-card',
        status: 'pending',
      },
      {
        key: 'provisioning',
        label: 'Provisionamento',
        description: 'Configurando seu portal e espaço de trabalho',
        icon: 'pi pi-server',
        status: 'pending',
      },
      {
        key: 'ready',
        label: 'Pronto!',
        description: 'Sua imobiliária está ativa e pronta para uso',
        icon: 'pi pi-check-circle',
        status: 'pending',
      },
    ];

    if (state === 'pending_payment') {
      ordered[0].status = 'active';
    } else if (state === 'awaiting_approval') {
      ordered[0].status = 'complete';
      ordered[1].status = 'active';
      ordered[1].description = 'Aguardando aprovação do administrador';
    } else if (state === 'provisioning') {
      ordered[0].status = 'complete';
      ordered[1].status = 'active';
    } else if (state === 'active') {
      ordered.forEach((s) => (s.status = 'complete'));
    }

    return ordered;
  }

  protected get dashboardUrl(): string {
    const slug = this.status()?.slug;
    if (!slug || !isPlatformBrowser(this.platformId)) return '/dashboard';
    const origin = window.location.origin;
    const base = origin.replace(/^(https?:\/\/)/, `$1${slug}.`);
    return base + '/dashboard';
  }
}
