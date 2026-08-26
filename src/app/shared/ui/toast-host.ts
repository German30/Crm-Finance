import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ToastService } from '../../core/services/toast.service';
import { Icon } from './icon';

@Component({
  selector: 'app-toast-host',
  standalone: true,
  imports: [Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="toast-host" aria-live="polite" aria-atomic="false">
      @for (toast of toasts.toasts(); track toast.id) {
        <div class="toast" [class]="'is-' + toast.kind" [class.is-leaving]="toast.leaving" role="status">
          <app-icon
            [name]="toast.kind === 'success' ? 'check' : toast.kind === 'error' ? 'alert' : 'info'"
          />
          <span class="toast-text">{{ toast.text }}</span>
          <button type="button" class="toast-close" (click)="toasts.dismiss(toast.id)" aria-label="Cerrar aviso">
            <app-icon name="close" [size]="14" />
          </button>
        </div>
      }
    </div>
  `,
})
export class ToastHost {
  readonly toasts = inject(ToastService);
}
