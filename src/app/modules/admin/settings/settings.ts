import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';
import { AccessService } from '../../../core/services/access.service';
import { ThemeService, ThemeChoice } from '../../../core/services/theme.service';
import { ToastService } from '../../../core/services/toast.service';
import { describeHttpError } from '../../../core/interceptors/error-interceptor';
import { environment } from '../../../../environments/environment';
import { Icon } from '../../../shared/ui/icon';
import { formatDate, formatTime, initials } from '../../../shared/utils/format';

const MIN_PASSWORD = 8;

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [FormsModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './settings.html',
  styleUrl: './settings.css',
})
export class Settings {
  private readonly auth = inject(AuthService);
  private readonly toast = inject(ToastService);
  protected readonly theme = inject(ThemeService);
  protected readonly access = inject(AccessService);

  protected readonly session = this.auth.session;
  protected readonly apiUrl = environment.apiUrl;
  protected readonly initials = initials;
  protected readonly minPassword = MIN_PASSWORD;

  protected readonly saving = signal(false);
  protected readonly formError = signal('');
  protected readonly showCurrent = signal(false);
  protected readonly showNew = signal(false);

  protected readonly passwords = { current: '', next: '', confirm: '' };

  protected readonly themeOptions: { value: ThemeChoice; label: string; hint: string; icon: string }[] = [
    { value: 'dark', label: 'Oscuro', hint: 'Para jornadas largas y salas con poca luz', icon: 'moon' },
    { value: 'light', label: 'Claro', hint: 'Para impresión y oficinas muy iluminadas', icon: 'sun' },
    { value: 'system', label: 'Del sistema', hint: 'Sigue la preferencia de tu equipo', icon: 'monitor' },
  ];

  protected readonly expiry = computed(() => {
    const expiresAt = this.session()?.expiresAt;
    if (!expiresAt) return null;
    return {
      date: formatDate(new Date(expiresAt)),
      time: formatTime(new Date(expiresAt)),
      minutesLeft: Math.max(0, Math.round((expiresAt - Date.now()) / 60_000)),
    };
  });

  /** Lo que este rol puede hacer, dicho en palabras y no en nombres de políticas. */
  protected readonly capabilities = computed(() => {
    const a = this.access;
    return [
      { label: 'Clientes, contratos y oportunidades', granted: true },
      { label: 'Catálogo de productos (consulta)', granted: true },
      { label: 'Detalle y movimientos de Banca', granted: a.canBanca() },
      { label: 'Detalle y siniestros de Seguros', granted: a.canSeguros() },
      { label: 'Gestión de usuarios', granted: a.canManageUsers() },
      { label: 'Alta y edición de productos', granted: a.canManageProducts() },
      { label: 'Eliminar registros', granted: a.canDelete() },
    ];
  });

  protected onChangePassword(form: NgForm): void {
    if (this.saving()) return;
    this.formError.set('');

    if (form.invalid) {
      form.control.markAllAsTouched();
      this.formError.set('Revisa los campos marcados antes de guardar.');
      return;
    }

    if (this.passwords.next !== this.passwords.confirm) {
      this.formError.set('La confirmación no coincide con la contraseña nueva.');
      return;
    }

    if (this.passwords.next === this.passwords.current) {
      this.formError.set('La contraseña nueva debe ser distinta de la actual.');
      return;
    }

    this.saving.set(true);
    this.auth
      .changePassword({
        CurrentPassword: this.passwords.current,
        NewPassword: this.passwords.next,
      })
      .subscribe({
        next: () => {
          this.saving.set(false);
          this.passwords.current = '';
          this.passwords.next = '';
          this.passwords.confirm = '';
          form.resetForm();
          this.toast.success('Tu contraseña quedó actualizada.');
        },
        error: (err) => {
          this.saving.set(false);
          this.formError.set(describeHttpError(err, 'No fue posible cambiar la contraseña.'));
        },
      });
  }
}
