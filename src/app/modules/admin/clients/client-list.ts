import { ChangeDetectionStrategy, Component, computed, inject, signal, viewChild } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CrmService } from '../../../core/services/crm.service';
import { AccessService } from '../../../core/services/access.service';
import { ToastService } from '../../../core/services/toast.service';
import { describeHttpError } from '../../../core/interceptors/error-interceptor';
import { CatalogItem, ClientGrid } from '../../../shared/models/api.model';
import { Icon } from '../../../shared/ui/icon';
import { ConfirmDialog } from '../../../shared/ui/confirm-dialog';
import { fold, formatNumber, initials } from '../../../shared/utils/format';

const PAGE_SIZE = 12;
type SortKey = 'clientName' | 'typePersonName' | 'assignedUserName';

@Component({
  selector: 'app-client-list',
  standalone: true,
  imports: [FormsModule, RouterLink, Icon, ConfirmDialog],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './client-list.html',
  styleUrl: './client-list.css',
})
export class ClientList {
  private readonly crm = inject(CrmService);
  private readonly toast = inject(ToastService);
  protected readonly access = inject(AccessService);

  private readonly dialog = viewChild.required(ConfirmDialog);

  protected readonly loading = signal(true);
  protected readonly errorText = signal('');
  protected readonly clients = signal<ClientGrid[]>([]);
  protected readonly typePersons = signal<CatalogItem[]>([]);

  protected readonly query = signal('');
  protected readonly typeFilter = signal<string>('');
  protected readonly sortKey = signal<SortKey>('clientName');
  protected readonly sortDesc = signal(false);
  protected readonly page = signal(1);
  protected readonly pending = signal<ClientGrid | null>(null);
  protected readonly deletingId = signal<number | null>(null);

  protected readonly fmtNumber = formatNumber;
  protected readonly initials = initials;
  protected readonly skeletonRows = [0, 1, 2, 3, 4, 5, 6, 7];

  protected readonly filtered = computed(() => {
    const needle = fold(this.query().trim());
    const type = this.typeFilter();

    const rows = this.clients().filter((c) => {
      if (type && c.typePersonName !== type) return false;
      if (!needle) return true;
      return (
        fold(c.clientName ?? '').includes(needle) ||
        fold(c.fiscalId ?? '').includes(needle) ||
        fold(c.email ?? '').includes(needle) ||
        fold(c.assignedUserName ?? '').includes(needle)
      );
    });

    const key = this.sortKey();
    const dir = this.sortDesc() ? -1 : 1;
    return [...rows].sort(
      (a, b) => String(a[key] ?? '').localeCompare(String(b[key] ?? ''), 'es') * dir,
    );
  });

  protected readonly totalPages = computed(() =>
    Math.max(1, Math.ceil(this.filtered().length / PAGE_SIZE)),
  );
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

  protected readonly hasFilters = computed(() => !!this.query().trim() || !!this.typeFilter());

  protected readonly moralCount = computed(
    () => this.clients().filter((c) => this.isMoral(c)).length,
  );

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorText.set('');
    forkJoin({
      clients: this.crm.getClients(),
      types: this.crm.getTypePersons().pipe(catchError(() => of([] as CatalogItem[]))),
    }).subscribe({
      next: ({ clients, types }) => {
        this.clients.set(Array.isArray(clients) ? clients : []);
        this.typePersons.set(types ?? []);
        this.loading.set(false);
      },
      error: (err) => {
        this.clients.set([]);
        this.errorText.set(describeHttpError(err, 'No fue posible cargar la cartera de clientes.'));
        this.loading.set(false);
      },
    });
  }

  /** El backend distingue persona física de moral por rutas distintas. */
  protected isMoral(client: ClientGrid): boolean {
    return (client.typePersonName ?? '').trim().toLowerCase().startsWith('moral');
  }

  protected detailLink(client: ClientGrid): string[] {
    return ['/admin/clients', this.isMoral(client) ? 'moral' : 'phisic', String(client.clinetId)];
  }

  protected sortBy(key: SortKey): void {
    if (this.sortKey() === key) this.sortDesc.set(!this.sortDesc());
    else {
      this.sortKey.set(key);
      this.sortDesc.set(false);
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
    this.typeFilter.set('');
    this.page.set(1);
  }

  protected askDelete(client: ClientGrid): void {
    this.pending.set(client);
    this.dialog().open();
  }

  protected onConfirmDelete(): void {
    const client = this.pending();
    if (!client) return;
    this.deletingId.set(client.clinetId);
    this.crm.deleteClient(client.clinetId).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.pending.set(null);
        this.toast.success(`${client.clientName} se eliminó de la cartera.`);
        this.load();
      },
      error: (err) => {
        this.deletingId.set(null);
        this.pending.set(null);
        this.toast.error(describeHttpError(err, 'No fue posible eliminar el cliente.'));
      },
    });
  }

  protected readonly dialogMessage = computed(() => {
    const c = this.pending();
    return c
      ? `Se eliminará a ${c.clientName} de la cartera. Si tiene contratos asociados, el servicio rechazará la operación.`
      : '';
  });
}
