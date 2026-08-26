import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CrmService } from '../../../core/services/crm.service';
import { AccessService } from '../../../core/services/access.service';
import { describeHttpError } from '../../../core/interceptors/error-interceptor';
import { Area, CatalogItem, ContractGrid } from '../../../shared/models/api.model';
import { isContractOpen } from '../../../core/services/crm-analytics';
import { Icon } from '../../../shared/ui/icon';
import { fold, formatAgo, formatDate, formatNumber } from '../../../shared/utils/format';

const PAGE_SIZE = 12;
type SortKey = 'referenceNumber' | 'clientName' | 'productName' | 'dateOpeningIssue';

@Component({
  selector: 'app-contract-list',
  standalone: true,
  imports: [FormsModule, RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './contract-list.html',
  styleUrl: './contract-list.css',
})
export class ContractList {
  private readonly crm = inject(CrmService);
  protected readonly access = inject(AccessService);

  protected readonly loading = signal(true);
  protected readonly errorText = signal('');
  protected readonly contracts = signal<ContractGrid[]>([]);
  protected readonly areas = signal<Area[]>([]);
  protected readonly statuses = signal<CatalogItem[]>([]);

  protected readonly query = signal('');
  protected readonly areaFilter = signal('');
  protected readonly statusFilter = signal('');
  protected readonly sortKey = signal<SortKey>('dateOpeningIssue');
  protected readonly sortDesc = signal(true);
  protected readonly page = signal(1);

  protected readonly fmtNumber = formatNumber;
  protected readonly fmtDate = formatDate;
  protected readonly fmtAgo = formatAgo;
  protected readonly skeletonRows = [0, 1, 2, 3, 4, 5, 6, 7];

  protected readonly filtered = computed(() => {
    const needle = fold(this.query().trim());
    const area = this.areaFilter();
    const status = this.statusFilter();

    const rows = this.contracts().filter((c) => {
      if (area && c.areaName !== area) return false;
      if (status && c.contractStatusName !== status) return false;
      if (!needle) return true;
      return (
        fold(c.referenceNumber ?? '').includes(needle) ||
        fold(c.clientName ?? '').includes(needle) ||
        fold(c.productName ?? '').includes(needle)
      );
    });

    const key = this.sortKey();
    const dir = this.sortDesc() ? -1 : 1;
    return [...rows].sort((a, b) => {
      if (key === 'dateOpeningIssue') {
        const ta = Date.parse(a.dateOpeningIssue ?? '');
        const tb = Date.parse(b.dateOpeningIssue ?? '');
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
    () => !!this.query().trim() || !!this.areaFilter() || !!this.statusFilter(),
  );

  protected readonly openCount = computed(() => this.filtered().filter(isContractOpen).length);

  /** Cuántos de los visibles no puede abrir este rol, para decirlo en claro. */
  protected readonly lockedCount = computed(
    () => this.filtered().filter((c) => !this.canOpen(c)).length,
  );

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorText.set('');
    forkJoin({
      contracts: this.crm.getContracts(),
      areas: this.crm.getAreas().pipe(catchError(() => of([] as Area[]))),
      statuses: this.crm.getContractStatuses().pipe(catchError(() => of([] as CatalogItem[]))),
    }).subscribe({
      next: ({ contracts, areas, statuses }) => {
        this.contracts.set(Array.isArray(contracts) ? contracts : []);
        this.areas.set(areas ?? []);
        this.statuses.set(statuses ?? []);
        const preset = this.access.defaultAreaFilter();
        if (preset && !this.areaFilter()) this.areaFilter.set(preset);
        this.loading.set(false);
      },
      error: (err) => {
        this.contracts.set([]);
        this.errorText.set(describeHttpError(err, 'No fue posible cargar los contratos.'));
        this.loading.set(false);
      },
    });
  }

  protected isBanca(contract: ContractGrid): boolean {
    return (contract.areaName ?? '').trim().toLowerCase() === 'banca';
  }

  /** El detalle exige BancaOAdministrador o SegurosOAdministrador. */
  protected canOpen(contract: ContractGrid): boolean {
    return this.isBanca(contract) ? this.access.canBanca() : this.access.canSeguros();
  }

  protected detailLink(contract: ContractGrid): string[] {
    return ['/admin/contracts', this.isBanca(contract) ? 'bank' : 'insurance', String(contract.contractId)];
  }

  protected statusClass(status: string): string {
    const s = (status ?? '').toLowerCase();
    if (s.includes('activ') || s.includes('vigente')) return 'badge badge-ok';
    if (s.includes('proceso') || s.includes('tramite') || s.includes('trámite')) return 'badge badge-warn';
    if (s.includes('cancel') || s.includes('rechaz') || s.includes('vencid')) return 'badge badge-down';
    return 'badge badge-off';
  }

  protected sortBy(key: SortKey): void {
    if (this.sortKey() === key) this.sortDesc.set(!this.sortDesc());
    else {
      this.sortKey.set(key);
      this.sortDesc.set(key === 'dateOpeningIssue');
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
    this.areaFilter.set('');
    this.statusFilter.set('');
    this.page.set(1);
  }
}
