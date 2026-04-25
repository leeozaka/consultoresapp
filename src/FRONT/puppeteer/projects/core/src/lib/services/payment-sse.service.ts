import { Injectable, inject, NgZone, PLATFORM_ID, signal } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { AppErrorHandlerService } from '../errors/error-handler.service';
import { environment } from '../environments/environment';

export interface PaymentEvent {
  tenantId: string;
  tenantName: string;
  planName: string;
  amount: number;
  currency: string;
  status: string;
  occurredAt: string;
  detail?: string;
  nextPaymentDate?: string;
  subscriptionStatus?: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentSseService {
  private readonly platformId = inject(PLATFORM_ID);
  private readonly zone = inject(NgZone);
  private readonly notifier = inject(AppErrorHandlerService);

  private eventSource: EventSource | null = null;
  private reconnectTimer: ReturnType<typeof setTimeout> | null = null;

  readonly lastEvent = signal<PaymentEvent | null>(null);

  connect(): void {
    if (!isPlatformBrowser(this.platformId)) return;
    this.disconnect();

    const base = environment.apiBaseUrl || '';
    const url = `${base}/api/tenant/payments/events`;

    this.eventSource = this.zone.runOutsideAngular(
      () => new EventSource(url, { withCredentials: true }),
    );

    this.eventSource.addEventListener('payment-status', (e: MessageEvent) => {
      try {
        const raw = JSON.parse(e.data);
        const evt: PaymentEvent = {
          tenantId: raw.tenant_id ?? raw.tenantId,
          tenantName: raw.tenant_name ?? raw.tenantName,
          planName: raw.plan_name ?? raw.planName,
          amount: raw.amount,
          currency: raw.currency,
          status: raw.status,
          occurredAt: raw.occurred_at ?? raw.occurredAt,
          detail: raw.detail,
          nextPaymentDate: raw.next_payment_date ?? raw.nextPaymentDate,
          subscriptionStatus: raw.subscription_status ?? raw.subscriptionStatus,
        };

        this.zone.run(() => {
          this.lastEvent.set(evt);
          this.showToast(evt);
        });
      } catch { /* ignore malformed events */ }
    });

    this.eventSource.onerror = () => {
      if (this.eventSource?.readyState === EventSource.CLOSED) {
        this.scheduleReconnect();
      }
    };
  }

  disconnect(): void {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
    this.eventSource?.close();
    this.eventSource = null;
  }

  private scheduleReconnect(): void {
    this.disconnect();
    this.reconnectTimer = setTimeout(() => this.connect(), 5000);
  }

  private showToast(evt: PaymentEvent): void {
    const amountFormatted = (evt.amount / 100).toLocaleString('pt-BR', {
      style: 'currency',
      currency: evt.currency?.toUpperCase() || 'BRL',
    });

    switch (evt.status) {
      case 'payment_succeeded':
        this.notifier.notify(
          'success',
          $localize`:@@payment.toast.succeeded:Pagamento confirmado`,
          $localize`:@@payment.toast.succeededDetail:${amountFormatted} processado com sucesso via Stripe.`,
        );
        break;

      case 'payment_expired':
        this.notifier.notify(
          'warn',
          $localize`:@@payment.toast.expired:Sessão expirada`,
          $localize`:@@payment.toast.expiredDetail:A sessão de checkout expirou. Tente novamente.`,
        );
        break;

      case 'invoice_paid':
        this.notifier.notify(
          'success',
          $localize`:@@payment.toast.invoicePaid:Fatura paga`,
          $localize`:@@payment.toast.invoicePaidDetail:Cobrança mensal de ${amountFormatted} processada.`,
        );
        break;

      case 'invoice_failed':
        this.notifier.notify(
          'error',
          $localize`:@@payment.toast.invoiceFailed:Falha no pagamento`,
          $localize`:@@payment.toast.invoiceFailedDetail:Não foi possível cobrar ${amountFormatted}. Atualize seus dados de pagamento.`,
        );
        break;

      case 'activated':
        this.notifier.notify(
          'success',
          $localize`:@@payment.toast.activated:Assinatura ativada`,
          $localize`:@@payment.toast.activatedDetail:Seu plano foi ativado com sucesso.`,
        );
        break;

      case 'subscription_updated':
        this.notifier.notify(
          'info',
          $localize`:@@payment.toast.subscriptionUpdated:Assinatura atualizada`,
          evt.detail || $localize`:@@payment.toast.subscriptionUpdatedDetail:As informações da sua assinatura foram sincronizadas.`,
        );
        break;

      case 'subscription_canceled':
        this.notifier.notify(
          'warn',
          $localize`:@@payment.toast.subscriptionCanceled:Assinatura cancelada`,
          evt.detail || $localize`:@@payment.toast.subscriptionCanceledDetail:O cancelamento da sua assinatura foi confirmado.`,
        );
        break;

      default:
        this.notifier.notify(
          'info',
          $localize`:@@payment.toast.generic:Atualização de pagamento`,
          evt.detail || `Status: ${evt.status}`,
        );
    }
  }
}
