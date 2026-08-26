import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CrmService } from '../../../core/services/crm.service';
import { AccessService } from '../../../core/services/access.service';
import { describeHttpError } from '../../../core/interceptors/error-interceptor';
import { Area, FinanceProduct } from '../../../shared/models/api.model';
import { Icon } from '../../../shared/ui/icon';
import { fold, formatNumber } from '../../../shared/utils/format';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [FormsModule, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './product-list.html',
  styleUrl: './product-list.css',
})
export class ProductList {
  private readonly crm = inject(CrmService);
  protected readonly access = inject(AccessService);

  protected readonly loading = signal(true);
  protected readonly errorText = signal('');
  protected readonly products = signal<FinanceProduct[]>([]);
  protected readonly areas = signal<Area[]>([]);

  protected readonly query = signal('');
  protected readonly areaFilter = signal('');

  protected readonly fmtNumber = formatNumber;
  protected readonly skeletonRows = [0, 1, 2, 3, 4, 5];

  protected readonly filtered = computed(() => {
    const needle = fold(this.query().trim());
    const area = this.areaFilter();
    return this.products().filter((p) => {
      if (area && p.areaName !== area) return false;
      if (!needle) return true;
      return fold(p.productName ?? '').includes(needle) || fold(p.description ?? '').includes(needle);
    });
  });

  protected readonly hasFilters = computed(() => !!this.query().trim() || !!this.areaFilter());

  protected readonly activeCount = computed(
    () => this.products().filter((p) => this.isActive(p)).length,
  );

  /** Las tasas conviven con primas; la unidad depende del área. */
  protected rateLabel(product: FinanceProduct): string {
    return (product.areaName ?? '').trim().toLowerCase() === 'seguros' ? 'Prima base' : 'Tasa';
  }

  constructor() {
    this.load();
  }

  protected load(): void {
    this.loading.set(true);
    this.errorText.set('');
    forkJoin({
      products: this.crm.getProducts(),
      areas: this.crm.getAreas().pipe(catchError(() => of([] as Area[]))),
    }).subscribe({
      next: ({ products, areas }) => {
        this.products.set(Array.isArray(products) ? products : []);
        this.areas.set(areas ?? []);
        // Un usuario de área aterriza viendo lo suyo, sin filtrar a mano.
        const preset = this.access.defaultAreaFilter();
        if (preset && !this.areaFilter()) this.areaFilter.set(preset);
        this.loading.set(false);
      },
      error: (err) => {
        this.products.set([]);
        this.errorText.set(describeHttpError(err, 'No fue posible cargar el catálogo de productos.'));
        this.loading.set(false);
      },
    });
  }

  protected isActive(product: FinanceProduct): boolean {
    return (product.statusName ?? '').trim().toLowerCase().startsWith('activ');
  }

  protected clearFilters(): void {
    this.query.set('');
    this.areaFilter.set('');
  }

  protected formatRate(value: number): string {
    if (!Number.isFinite(value)) return '—';
    return value.toFixed(2).replace(/\.00$/, '') + '%';
  }
}
