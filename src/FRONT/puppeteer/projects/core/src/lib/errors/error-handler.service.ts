import { Injectable, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';

export type NotificationSeverity = 'success' | 'info' | 'warn' | 'error';

export interface AppNotification {
  id: string;
  severity: NotificationSeverity;
  summary: string;
  detail?: string;
}

@Injectable({ providedIn: 'root' })
export class AppErrorHandlerService {
  private readonly _notifications = signal<AppNotification[]>([]);
  readonly notifications = this._notifications.asReadonly();

  handleError(error: unknown): void {
    const notification = this.toNotification(error);
    this._notifications.update((list) => [...list, notification]);
  }

  dismiss(id: string): void {
    this._notifications.update((list) => list.filter((n) => n.id !== id));
  }

  notify(severity: NotificationSeverity, summary: string, detail?: string): void {
    this._notifications.update((list) => [
      ...list,
      { id: this.generateId(), severity, summary, detail },
    ]);
  }

  private toNotification(error: unknown): AppNotification {
    if (error instanceof HttpErrorResponse) {
      return this.fromHttpError(error);
    }
    return {
      id: this.generateId(),
      severity: 'error',
      summary: 'Erro inesperado',
      detail: error instanceof Error ? error.message : String(error),
    };
  }

  private fromHttpError(err: HttpErrorResponse): AppNotification {
    const id = this.generateId();
    switch (err.status) {
      case 401:
        return { id, severity: 'warn', summary: 'Erro de autenticação', detail: 'Faça login novamente.' };
      case 403:
        return { id, severity: 'warn', summary: 'Sem permissão', detail: 'Você não tem acesso a este recurso.' };
      case 404:
        return { id, severity: 'info', summary: 'Não encontrado', detail: 'O recurso solicitado não foi encontrado.' };
      case 422:
        return { id, severity: 'warn', summary: 'Dados inválidos', detail: this.extractValidationMessage(err) };
      case 429:
        return { id, severity: 'warn', summary: 'Muitas requisições', detail: 'Aguarde um momento antes de tentar novamente.' };
      default:
        return { id, severity: 'error', summary: 'Erro no servidor', detail: 'Ocorreu um erro inesperado. Tente novamente.' };
    }
  }

  private extractValidationMessage(err: HttpErrorResponse): string {
    return err.error?.detail ?? err.error?.message ?? 'Verifique os dados enviados.';
  }

  private generateId(): string {
    return Math.random().toString(36).substring(2);
  }
}
