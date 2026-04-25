import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { AppErrorHandlerService, AppNotification } from '../../errors/error-handler.service';

@Component({
  selector: 'app-toast',
  templateUrl: './toast.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ToastComponent {
  protected readonly errorHandler = inject(AppErrorHandlerService);
  protected readonly notifications = this.errorHandler.notifications;

  protected dismiss(id: string): void {
    this.errorHandler.dismiss(id);
  }

  protected trackById(_: number, n: AppNotification): string {
    return n.id;
  }

  protected severityColor(severity: string): string {
    const map: Record<string, string> = {
      success: '#22c55e',
      info:    '#3b82f6',
      warn:    '#f59e0b',
      error:   '#ef4444',
    };
    return map[severity] ?? '#6b7280';
  }
}
