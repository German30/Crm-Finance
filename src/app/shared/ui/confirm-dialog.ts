import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';

/**
 * A real `<dialog>`: focus trap, Esc, and inert background come from the
 * platform rather than from a hand-rolled overlay.
 */
@Component({
  selector: 'app-confirm-dialog',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <dialog #dlg class="modal" (close)="onNativeClose()">
      <div class="modal-body">
        <h3>{{ title() }}</h3>
        <p>{{ message() }}</p>
      </div>
      <div class="modal-foot">
        <button type="button" class="btn btn-secondary" (click)="close(false)">
          {{ cancelLabel() }}
        </button>
        <button
          type="button"
          [class]="danger() ? 'btn btn-danger' : 'btn btn-primary'"
          (click)="close(true)"
        >
          {{ confirmLabel() }}
        </button>
      </div>
    </dialog>
  `,
})
export class ConfirmDialog {
  readonly title = input('¿Confirmar?');
  readonly message = input('');
  readonly confirmLabel = input('Confirmar');
  readonly cancelLabel = input('Cancelar');
  readonly danger = input(false);

  readonly confirmed = output<void>();
  readonly cancelled = output<void>();

  private readonly dlg = viewChild.required<ElementRef<HTMLDialogElement>>('dlg');
  private result = false;

  open(): void {
    this.result = false;
    const el = this.dlg().nativeElement;
    if (!el.open) el.showModal();
  }

  protected close(confirm: boolean): void {
    this.result = confirm;
    this.dlg().nativeElement.close();
  }

  protected onNativeClose(): void {
    if (this.result) this.confirmed.emit();
    else this.cancelled.emit();
    this.result = false;
  }
}
