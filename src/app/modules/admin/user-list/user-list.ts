import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { CrmService } from '../../../core/services/crm.service';
import { ToastService } from '../../../core/services/toast.service';
import { AuthService } from '../../../core/services/auth.service';
import { describeHttpError } from '../../../core/interceptors/error-interceptor';
import { UserResponse } from '../../../shared/models/api.model';
import { Icon } from '../../../shared/ui/icon';
import { ConfirmDialog } from '../../../shared/ui/confirm-dialog';
import { fold, formatDate, formatNumber, initials } from '../../../shared/utils/format';


type SortKey = 'name' | 'roleName' | 'areaName' | 'creationDate' | 'statusName';

const PAGE_SIZE = 12;

@Component({
  selector: 'app-user-list',
  standalone: true,
  imports: [FormsModule, RouterLink, Icon, ConfirmDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './user-list.html',
  styleUrl: './user-list.css',
})
export class UserList {
  private readonly crm = inject(CrmService);
  private readonly toast = inject(ToastService);
  private readonly auth = inject(AuthService);

  private readonly dialog = viewChild.required(ConfirmDialog);

  protected readonly users = signal<UserResponse[]>([]);
  protected readonly loading = signal(true);
  protected readonly errorText = signal('');
  protected readonly query = signal('');
  protected readonly statusFilter = signal<'' | 'activos' | 'inactivos'>('');
  protected readonly sortKey = signal<SortKey>('creationDate');
  protected readonly sortDesc = signal(true);
  protected readonly page = signal(1);
  protected readonly togglingId = signal<number | null>(null);

  /** The row awaiting confirmation; also drives the dialog copy. */
  protected readonly pending = signal<UserResponse | null>(null);

  protected readonly fmtDate = formatDate;
  protected readonly fmtNumber = formatNumber;
  protected readonly initials = initials;
  protected readonly skeletonRows = [0, 1, 2, 3, 4, 5];

  protected readonly currentEmail = computed(() =>
    (this.auth.session()?.email ?? '').toLowerCase(),
  );

  protected readonly filtered = computed(() => {
    const needle = fold(this.query().trim());
    const state = this.statusFilter();

    const rows = this.users().filter((u) => {
      if (state === 'activos' && !this.isActive(u)) return false;
      if (state === 'inactivos' && this.isActive(u)) return false;
      if (!needle) return true;
      return (
        fold(u.name ?? '').includes(needle) ||
        fold(u.email ?? '').includes(needle) ||
        fold(u.roleName ?? '').includes(needle) ||
        fold(u.areaName ?? '').includes(needle)
      );
    });

    const key = this.sortKey();
    const dir = this.sortDesc() ? -1 : 1;
    return [...rows].sort((a, b) => {
      if (key === 'creationDate') {
        const ta = Date.parse(a.creationDate ?? '');
        const tb = Date.parse(b.creationDate ?? '');
        // Undated rows sort last whichever way the column points.
        if (Number.isNaN(ta) && Number.isNaN(tb)) return 0;
        if (Number.isNaN(ta)) return 1;
        if (Number.isNaN(tb)) return -1;
        return (ta - tb) * dir;
      }
      return String(a[key] ?? '').localeCompare(String(b[key] ?? ''), 'es') * dir;
    });
  });

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filtered().length / PAGE_SIZE)),
  );

  /** Clamped to the live result set, so filtering never strands an empty page. */
  protected readonly currentPage = computed(() => Math.min(this.page(), this.totalPages()));

  protected readonly pageRows = computed(() => {
    const start = (this.currentPage() - 1) * PAGE_SIZE;
    return this.filtered().slice(start, start + PAGE_SIZE);
  });

  protected readonly rangeLabel = computed(() => {
    const total = this.filtered().length;
    if (!total) return '0 resultados';
    const start = (this.currentPage() - 1) * PAGE_SIZE + 1;
    return `${start}–${Math.min(start + PAGE_SIZE - 1, total)} de ${formatNumber(total)}`;
  });

  protected readonly pageNumbers = computed(() => {
    const total = this.totalPages();
    let start = Math.max(1, this.currentPage() - 2);
    const end = Math.min(total, start + 4);
    start = Math.max(1, end - 4);
    return Array.from({ length: end - start + 1 }, (_, i) => start + i);
  });

  protected readonly hasFilters = computed(
    () => !!this.query().trim() || !!this.statusFilter(),
  );

  protected readonly activeCount = computed(
    () => this.users().filter((u) => this.isActive(u)).length,
  );

  protected readonly dialogMessage = computed(() => {
    const user = this.pending();
    if (!user) return '';
    return this.isActive(user)
      ? `${user.name} perderá el acceso al panel de inmediato. Podrás reactivarlo cuando lo necesites.`
      : `${user.name} recuperará el acceso al panel con su rol actual (${user.roleName}).`;
  });

  protected readonly dialogTitle = computed(() => {
    const user = this.pending();
    if (!user) return 'Confirmar';
    return this.isActive(user) ? 'Dar de baja el usuario' : 'Reactivar el usuario';
  });

  protected readonly dialogConfirmLabel = computed(() => {
    const user = this.pending();
    if (!user) return 'Confirmar';
    return this.isActive(user) ? 'Dar de baja' : 'Reactivar';
  });

  constructor() {
    this.loadUsers();
  }

  protected loadUsers(): void {
    this.loading.set(true);
    this.errorText.set('');
    this.crm.getUsers().subscribe({
      next: (data) => {
        this.users.set(Array.isArray(data) ? data : []);
        this.loading.set(false);
      },
      error: (err) => {
        this.users.set([]);
        this.errorText.set(
          describeHttpError(err, 'No fue posible cargar el directorio de usuarios.'),
        );
        this.loading.set(false);
      },
    });
  }

  protected isActive(user: UserResponse): boolean {
    return (user.statusName ?? '').trim().toLowerCase().startsWith('activ');
  }

  protected sortBy(key: SortKey): void {
    if (this.sortKey() === key) {
      this.sortDesc.set(!this.sortDesc());
    } else {
      this.sortKey.set(key);
      // Dates open newest-first; text columns open A–Z.
      this.sortDesc.set(key === 'creationDate');
    }
    this.page.set(1);
  }

  protected sortMark(key: SortKey): string {
    return this.sortKey() === key && !this.sortDesc() ? '↑' : '↓';
  }

  protected ariaSort(key: SortKey): 'ascending' | 'descending' | 'none' {
    if (this.sortKey() !== key) return 'none';
    return this.sortDesc() ? 'descending' : 'ascending';
  }

  protected onFilterChange(): void {
    this.page.set(1);
  }

  protected clearFilters(): void {
    this.query.set('');
    this.statusFilter.set('');
    this.page.set(1);
  }

  protected isSelf(user: UserResponse): boolean {
    const email = this.currentEmail();
    return !!email && (user.email ?? '').toLowerCase() === email;
  }

  protected askToggle(user: UserResponse): void {
    this.pending.set(user);
    this.dialog().open();
  }

  protected onConfirm(): void {
    const user = this.pending();
    if (!user) return;

    const wasActive = this.isActive(user);
    this.togglingId.set(user.userId);

    this.crm.toggleUserStatus(user.userId).subscribe({
      next: () => {
        this.togglingId.set(null);
        this.pending.set(null);
        this.toast.success(
          wasActive ? `${user.name} quedó dado de baja.` : `${user.name} fue reactivado.`,
        );
        this.loadUsers();
      },
      error: (err) => {
        this.togglingId.set(null);
        this.pending.set(null);
        this.toast.error(describeHttpError(err, 'No fue posible cambiar el estado del usuario.'));
      },
    });
  }

  protected onCancel(): void {
    this.pending.set(null);
  }
}
