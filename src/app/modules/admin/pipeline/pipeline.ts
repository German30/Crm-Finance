import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CrmService } from '../../../core/services/crm.service';
import { AccessService } from '../../../core/services/access.service';
import { ToastService } from '../../../core/services/toast.service';
import { describeHttpError } from '../../../core/interceptors/error-interceptor';
import { Area, CatalogItem, OportunityResponse } from '../../../shared/models/api.model';
import {
  isOportunityOpen,
  isOportunityWon,
  overdueOportunities,
  pipelineValue,
  weightedPipeline,
} from '../../../core/services/crm-analytics';
import { Icon } from '../../../shared/ui/icon';
import { fold, formatCompact, formatDate, formatMoney, formatNumber } from '../../../shared/utils/format';

interface Column {
  stage: CatalogItem;
  rows: OportunityResponse[];
  total: number;
}

@Component({
  selector: 'app-pipeline',
  standalone: true,
  imports: [FormsModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './pipeline.html',
  styleUrl: './pipeline.css',
})
export class Pipeline {
  private readonly crm = inject(CrmService);
  private readonly toast = inject(ToastService);
  protected readonly access = inject(AccessService);

  protected readonly loading = signal(true);
  protected readonly errorText = signal('');
  protected readonly oportunities = signal<OportunityResponse[]>([]);
  protected readonly stages = signal<CatalogItem[]>([]);
  protected readonly areas = signal<Area[]>([]);

  protected readonly query = signal('');
  protected readonly areaFilter = signal('');
  protected readonly movingId = signal<number | null>(null);

  protected readonly fmtMoney = formatMoney;
  protected readonly fmtCompact = formatCompact;
  protected readonly fmtNumber = formatNumber;
  protected readonly fmtDate = formatDate;

  protected readonly filtered = computed(() => {
    const needle = fold(this.query().trim());
    const area = this.areaFilter();
    return this.oportunities().filter((o) => {
      if (area && o.areaName !== area) return false;
      if (!needle) return true;
      return (
        fold(o.clientName ?? '').includes(needle) ||
        fold(o.prdoductName ?? '').includes(needle) ||
        fold(o.assignedUserName ?? '').includes(needle)
      );
    });
  });

  /** Una columna por etapa del catálogo, en el orden que define el backend. */
  protected readonly columns = computed<Column[]>(() => {
    const rows = this.filtered();
    const stages = this.stages();
    if (!stages.length) return [];
    return stages.map((stage) => {
      const inStage = rows.filter((o) => o.stageName === stage.name);
      return {
        stage,
        rows: inStage,
        total: inStage.reduce((sum, o) => sum + (o.estimatedMont ?? 0), 0),
      };
    });
  });

  /** Oportunidades cuya etapa no está en el catálogo: no deben desaparecer. */
  protected readonly orphans = computed(() => {
    const known = new Set(this.stages().map((s) => s.name));
    return this.filtered().filter((o) => !known.has(o.stageName));
  });

  protected readonly openCount = computed(() => this.filtered().filter(isOportunityOpen).length);
  protected readonly wonCount = computed(() => this.filtered().filter(isOportunityWon).length);
  protected readonly overdue = computed(() => overdueOportunities(this.filtered()));
  protected readonly total = computed(() => pipelineValue(this.filtered()));
  protected readonly weighted = computed(() => weightedPipeline(this.filtered()));

  protected readonly hasFilters = computed(() => !!this.query().trim() || !!this.areaFilter());
  protected readonly hasData = computed(() => this.oportunities().length > 0);

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorText.set('');
    forkJoin({
      oportunities: this.crm.getOportunities(),
      stages: this.crm.getStages().pipe(catchError(() => of([] as CatalogItem[]))),
      areas: this.crm.getAreas().pipe(catchError(() => of([] as Area[]))),
    }).subscribe({
      next: ({ oportunities, stages, areas }) => {
        this.oportunities.set(Array.isArray(oportunities) ? oportunities : []);
        this.stages.set(stages ?? []);
        this.areas.set(areas ?? []);
        const preset = this.access.defaultAreaFilter();
        if (preset && !this.areaFilter()) this.areaFilter.set(preset);
        this.loading.set(false);
      },
      error: (err) => {
        this.oportunities.set([]);
        this.errorText.set(describeHttpError(err, 'No fue posible cargar el embudo.'));
        this.loading.set(false);
      },
    });
  }

  /** PATCH /api/Oportunity/{id}/stage — sin política: cualquier rol autenticado. */
  protected moveTo(oportunity: OportunityResponse, stageId: number | string): void {
    const id = Number(stageId);
    if (!Number.isInteger(id) || id <= 0) return;

    // ngModel emite también al pintar; si la etapa no cambia, no hay nada que pedir.
    const actual = this.stages().find((s) => s.name === oportunity.stageName);
    if (actual && actual.id === id) return;

    this.movingId.set(oportunity.oportunityId);
    this.crm.changeOportunityStage(oportunity.oportunityId, { StageId: id }).subscribe({
      next: (updated) => {
        this.movingId.set(null);
        // Se sustituye la fila con lo que devolvió el backend, no con lo que asumimos.
        this.oportunities.update((list) =>
          list.map((o) => (o.oportunityId === updated.oportunityId ? updated : o)),
        );
        this.toast.success(`${updated.clientName} pasó a «${updated.stageName}».`);
      },
      error: (err) => {
        this.movingId.set(null);
        this.toast.error(describeHttpError(err, 'No fue posible cambiar la etapa.'));
      },
    });
  }

  protected clearFilters(): void {
    this.query.set('');
    this.areaFilter.set('');
  }

  protected isOverdue(o: OportunityResponse): boolean {
    if (!isOportunityOpen(o) || !o.dateEstimatedClose) return false;
    const close = new Date(o.dateEstimatedClose);
    return !Number.isNaN(close.getTime()) && close.getTime() < Date.now();
  }

  /** La probabilidad tiñe la barra: es la señal que el asesor mira primero. */
  protected probabilityTone(value: number): string {
    if (value >= 70) return 'is-high';
    if (value >= 40) return 'is-mid';
    return 'is-low';
  }
}
