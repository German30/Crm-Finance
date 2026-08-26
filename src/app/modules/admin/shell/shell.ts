import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  HostListener,
  computed,
  inject,
  signal,
} from '@angular/core';
import { NavigationEnd, Router, RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { filter } from 'rxjs';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { AccessService } from '../../../core/services/access.service';
import { Icon } from '../../../shared/ui/icon';
import { ConfirmDialog } from '../../../shared/ui/confirm-dialog';
import { initials } from '../../../shared/utils/format';

interface NavItem {
  path: string;
  label: string;
  icon: string;
  /** Si devuelve false, el rol no puede entrar y el enlace no se pinta. */
  visible: () => boolean;
}

@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, Icon, ConfirmDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './shell.html',
  styleUrl: './shell.css',
})
export class Shell {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected readonly theme = inject(ThemeService);

  protected readonly access = inject(AccessService);

  // La navegación se recorta al alcance del rol: un enlace que solo puede
  // devolver 403 es peor que no ofrecerlo.
  private readonly navAll: NavItem[] = [
    { path: '/admin/overview', label: 'Panorama', icon: 'overview', visible: () => true },
    { path: '/admin/clients', label: 'Clientes', icon: 'clients', visible: () => true },
    { path: '/admin/contracts', label: 'Contratos', icon: 'contracts', visible: () => true },
    { path: '/admin/pipeline', label: 'Oportunidades', icon: 'pipeline', visible: () => true },
    { path: '/admin/products', label: 'Productos', icon: 'products', visible: () => true },
    { path: '/admin/users', label: 'Usuarios', icon: 'users', visible: () => this.access.canManageUsers() },
    { path: '/admin/settings', label: 'Ajustes', icon: 'settings', visible: () => true },
  ];

  protected readonly nav = computed(() => this.navAll.filter((item) => item.visible()));

  protected readonly session = this.auth.session;
  protected readonly menuOpen = signal(false);
  protected readonly railOpen = signal(false);

  protected readonly initials = computed(() => initials(this.session()?.name ?? 'Usuario'));

  /** The desk clock — updates once a minute, not once a second. */
  private readonly tick = signal(new Date());
  protected readonly clock = computed(() => {
    const text = new Intl.DateTimeFormat('es-MX', {
      weekday: 'short',
      day: '2-digit',
      month: 'short',
      hour: '2-digit',
      minute: '2-digit',
    }).format(this.tick());
    // Spanish sentence case: only the first letter, so "de" stays lowercase.
    return text.charAt(0).toUpperCase() + text.slice(1);
  });

  constructor() {
    const destroyRef = inject(DestroyRef);
    const timer = setInterval(() => this.tick.set(new Date()), 60_000);
    destroyRef.onDestroy(() => clearInterval(timer));

    // Close the transient surfaces whenever the route changes.
    this.router.events
      .pipe(
        filter((e) => e instanceof NavigationEnd),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe(() => {
        this.menuOpen.set(false);
        this.railOpen.set(false);
      });
  }

  @HostListener('document:keydown.escape')
  protected onEscape(): void {
    this.menuOpen.set(false);
    this.railOpen.set(false);
  }

  protected confirmLogout(dialog: ConfirmDialog): void {
    this.menuOpen.set(false);
    dialog.open();
  }

  protected doLogout(): void {
    this.auth.logout();
    this.router.navigate(['/auth/login']);
  }
}
