import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule, NgForm } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { describeHttpError } from '../../../core/interceptors/error-interceptor';
import { LoginRequest } from '../../../shared/models/api.model';
import { Icon } from '../../../shared/ui/icon';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.html',
  styleUrl: './login.css',
})
export class Login {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  protected readonly theme = inject(ThemeService);

  protected readonly credentials: LoginRequest = { Email: '', Password: '' };
  protected readonly errorMessage = signal('');
  protected readonly isLoading = signal(false);
  protected readonly showPassword = signal(false);

  protected readonly year = new Date().getFullYear();

  protected onSubmit(form: NgForm): void {
    if (this.isLoading()) return;

    if (form.invalid) {
      form.control.markAllAsTouched();
      this.errorMessage.set('Captura tu correo y contraseña para continuar.');
      return;
    }

    this.isLoading.set(true);
    this.errorMessage.set('');

    this.auth
      .login({
        Email: this.credentials.Email.trim(),
        Password: this.credentials.Password,
      })
      .subscribe({
        next: () => {
          this.isLoading.set(false);
          const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');
          this.router.navigateByUrl(this.safeReturnUrl(returnUrl));
        },
        error: (err) => {
          this.isLoading.set(false);
          this.errorMessage.set(
            err?.status === 401
              ? 'Correo o contraseña incorrectos. Verifica tus datos e intenta de nuevo.'
              : describeHttpError(err, 'No fue posible iniciar sesión. Intenta de nuevo.'),
          );
        },
      });
  }

  /** Only same-origin app paths are honoured, never an absolute URL. */
  private safeReturnUrl(candidate: string | null): string {
    if (!candidate) return '/admin/overview';
    const isInternal = candidate.startsWith('/') && !candidate.startsWith('//');
    return isInternal ? candidate : '/admin/overview';
  }
}
