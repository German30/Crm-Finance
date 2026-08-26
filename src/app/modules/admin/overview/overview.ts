import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CrmService } from '../../../core/services/crm.service';
import { AccessService } from '../../../core/services/access.service';
import { AuthService } from '../../../core/services/auth.service';
import { describeHttpError } from '../../../core/interceptors/error-interceptor';
import {
  ClientGrid,
  ContractGrid,
  FinanceProduct,
  OportunityResponse,
} from '../../../shared/models/api.model';
import {
  closingSoon,
  contractsByArea,
  contractsByMonth,
  countBy,
  isContractOpen,
  isOportunityOpen,
  isOportunityWon,
  newestContracts,
  overdueOportunities,
  pipelineValue,
  weightedPipeline,
} from '../../../core/services/crm-analytics';
import { AreaChart } from '../../../shared/ui/area-chart';
import { BarChart } from '../../../shared/ui/bar-chart';
import { Icon } from '../../../shared/ui/icon';
import {
  formatAgo,
  formatCompact,
  formatDate,
  formatMoney,
  formatNumber,
} from '../../../shared/utils/format';

/** Orden fijo, nunca cíclico. La quinta categoría cae en «Otros». */
const SERIES = ['var(--series-1)', 'var(--series-2)', 'var(--series-3)', 'var(--series-4)'];
const MAX_SLICES = 4;

@Component({
  selector: 'app-overview',
  standalone: true,
  imports: [RouterLink, AreaChart, BarChart, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './overview.html',
  styleUrl: './overview.css',
})
export class Overview {
  private readonly crm = inject(CrmService);
  private readonly auth = inject(AuthService);
  protected readonly access = inject(AccessService);

  protected readonly loading = signal(true);
  protected readonly errorText = signal('');

  protected readonly clients = signal<ClientGrid[]>([]);
  protected readonly contracts = signal<ContractGrid[]>([]);
  protected readonly oportunities = signal<OportunityResponse[]>([]);
  protected readonly products = signal<FinanceProduct[]>([]);

  protected readonly fmtNumber = formatNumber;
  protected readonly fmtMoney = formatMoney;
  protected readonly fmtCompact = formatCompact;
  protected readonly fmtDate = formatDate;
  protected readonly fmtAgo = formatAgo;
  protected readonly skeletonRows = [0, 1, 2, 3, 4, 5];

  protected readonly greeting = computed(() => {
    const hour = new Date().getHours();
    const name = (this.auth.session()?.name ?? '').split(' ')[0];
    const time = hour < 12 ? 'Buenos días' : hour < 19 ? 'Buenas tardes' : 'Buenas noches';
    return name ? `${time}, ${name}` : time;
  });

  protected readonly today = new Intl.DateTimeFormat('es-MX', {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(new Date());

  /* ------------------------------------------------------------- cifras -- */

  protected readonly openContracts = computed(() => this.contracts().filter(isContractOpen));
  protected readonly openOportunities = computed(() => this.oportunities().filter(isOportunityOpen));
  protected readonly wonOportunities = computed(() => this.oportunities().filter(isOportunityWon));
  protected readonly overdue = computed(() => overdueOportunities(this.oportunities()));

  protected readonly pipelineTotal = computed(() => pipelineValue(this.oportunities()));
  protected readonly pipelineWeighted = computed(() => weightedPipeline(this.oportunities()));

  protected readonly winRate = computed(() => {
    const closed = this.oportunities().filter((o) => !isOportunityOpen(o)).length;
    return closed ? Math.round((this.wonOportunities().length / closed) * 100) : 0;
  });

  protected readonly activeProducts = computed(
    () => this.products().filter((p) => (p.statusName ?? '').toLowerCase().startsWith('activ')).length,
  );

  /* ------------------------------------------------------------ gráficas -- */

  protected readonly contractGrowth = computed(() => contractsByMonth(this.contracts(), 12));
  protected readonly contractIntake = computed(() => contractsByArea(this.contracts(), 6));

  protected readonly byStage = computed(() =>
    this.toRows(countBy(this.openOportunities(), (o) => o.stageName, 'Sin etapa')),
  );

  protected readonly byStatus = computed(() =>
    this.toRows(countBy(this.contracts(), (c) => c.contractStatusName, 'Sin estado')),
  );

  protected readonly closing = computed(() => closingSoon(this.oportunities(), 6));
  protected readonly recentContracts = computed(() => newestContracts(this.contracts(), 6));

  protected readonly hasContracts = computed(() => this.contracts().length > 0);
  protected readonly hasOportunities = computed(() => this.oportunities().length > 0);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorText.set('');

    // Todos estos endpoints solo piden [Authorize]: ningún rol se queda fuera.
    // Un módulo que falle degrada su panel, no la página entera.
    forkJoin({
      clients: this.crm.getClients().pipe(catchError(() => of([] as ClientGrid[]))),
      contracts: this.crm.getContracts().pipe(catchError(() => of([] as ContractGrid[]))),
      oportunities: this.crm.getOportunities().pipe(catchError(() => of([] as OportunityResponse[]))),
      products: this.crm.getProducts().pipe(catchError(() => of([] as FinanceProduct[]))),
    }).subscribe({
      next: ({ clients, contracts, oportunities, products }) => {
        this.clients.set(clients ?? []);
        this.contracts.set(contracts ?? []);
        this.oportunities.set(oportunities ?? []);
        this.products.set(products ?? []);
        this.loading.set(false);
      },
      error: (err) => {
        this.errorText.set(describeHttpError(err, 'No fue posible cargar el panorama.'));
        this.loading.set(false);
      },
    });
  }

  private toRows(slices: { name: string; value: number }[]) {
    const total = slices.reduce((sum, s) => sum + s.value, 0) || 1;
    const head = slices.slice(0, MAX_SLICES);
    const tail = slices.slice(MAX_SLICES);

    const rows = head.map((slice, i) => ({
      name: slice.name,
      value: slice.value,
      pct: (slice.value / total) * 100,
      color: SERIES[i],
    }));

    if (tail.length) {
      const rest = tail.reduce((sum, s) => sum + s.value, 0);
      rows.push({
        name: `Otros (${tail.length})`,
        value: rest,
        pct: (rest / total) * 100,
        color: 'var(--line-strong)',
      });
    }
    return rows;
  }

  protected statusClass(status: string): string {
    const s = (status ?? '').toLowerCase();
    if (s.includes('activ') || s.includes('vigente')) return 'badge badge-ok';
    if (s.includes('proceso') || s.includes('tramite') || s.includes('trámite')) return 'badge badge-warn';
    if (s.includes('cancel') || s.includes('rechaz') || s.includes('vencid')) return 'badge badge-down';
    return 'badge badge-off';
  }

  /** El detalle del contrato vive en una ruta distinta según el área. */
  protected contractLink(contract: ContractGrid): string[] {
    const area = (contract.areaName ?? '').trim().toLowerCase() === 'banca' ? 'bank' : 'insurance';
    return ['/admin/contracts', area, String(contract.contractId)];
  }

  /** Solo se enlaza el detalle si el rol puede abrirlo (política del backend). */
  protected canOpenContract(contract: ContractGrid): boolean {
    return (contract.areaName ?? '').trim().toLowerCase() === 'banca'
      ? this.access.canBanca()
      : this.access.canSeguros();
  }
}
