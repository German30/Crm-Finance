import { ChangeDetectionStrategy, Component, computed, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin, of } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { CrmService } from '../../../core/services/crm.service';
import { AccessService } from '../../../core/services/access.service';
import { describeHttpError } from '../../../core/interceptors/error-interceptor';
import {
  BankContractDetail,
  InsuranceClaimResponse,
  InsuranceContractDetail,
  TransactionResponse,
} from '../../../shared/models/api.model';
import { Icon } from '../../../shared/ui/icon';
import { formatDate, formatMoney, formatNumber } from '../../../shared/utils/format';

@Component({
  selector: 'app-contract-detail',
  standalone: true,
  imports: [RouterLink, Icon],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './contract-detail.html',
  styleUrl: './contract-detail.css',
})
export class ContractDetail {
  private readonly crm = inject(CrmService);
  protected readonly access = inject(AccessService);

  /** /admin/contracts/:area/:id — area es 'bank' o 'insurance'. */
  readonly area = input<string>();
  readonly id = input<string>();

  protected readonly loading = signal(true);
  protected readonly errorText = signal('');
  protected readonly bank = signal<BankContractDetail | null>(null);
  protected readonly insurance = signal<InsuranceContractDetail | null>(null);
  protected readonly transactions = signal<TransactionResponse[]>([]);
  protected readonly claims = signal<InsuranceClaimResponse[]>([]);
  protected readonly operationsDenied = signal(false);

  protected readonly fmtMoney = formatMoney;
  protected readonly fmtDate = formatDate;
  protected readonly fmtNumber = formatNumber;

  protected readonly isBank = computed(() => this.area() === 'bank');
  protected readonly found = computed(() => !!this.bank() || !!this.insurance());

  protected readonly head = computed(() => {
    const b = this.bank();
    if (b) {
      return {
        referenceNumber: b.referenceNumber,
        clientName: b.clientName,
        productName: b.productName,
        contractStatusName: b.contractStatusName,
        dateOpeningIssue: b.dateOpeningIssue,
        dateEnd: b.dateEnd,
      };
    }
    const i = this.insurance();
    return i
      ? {
          referenceNumber: i.referenceNumber,
          clientName: i.clientName,
          productName: i.productName,
          contractStatusName: i.contractStatusName,
          dateOpeningIssue: i.dateOpeningIssue,
          dateEnd: i.dateEnd,
        }
      : null;
  });

  /** Las cifras que encabezan el contrato, ya en la unidad correcta. */
  protected readonly figures = computed(() => {
    const b = this.bank();
    if (b) {
      return [
        { label: 'Saldo actual', value: formatMoney(b.balanceActual), unit: 'MXN' },
        { label: 'Monto otorgado', value: formatMoney(b.loanAmountGranted), unit: 'MXN' },
        { label: 'Tasa pactada', value: b.agreedInterestRate.toFixed(2) + '%', unit: '' },
        { label: 'Día de corte', value: String(b.monthlyCotoffDay), unit: 'del mes' },
      ];
    }
    const i = this.insurance();
    if (!i) return [];
    return [
      { label: 'Suma asegurada', value: formatMoney(i.insuranceSumeTotal), unit: 'MXN' },
      { label: 'Prima anual', value: formatMoney(i.totalAnnualPremium), unit: 'MXN' },
      { label: 'Deducible', value: i.porcentDeductible.toFixed(2) + '%', unit: '' },
      { label: 'Forma de pago', value: i.payFromName, unit: '' },
    ];
  });

  protected readonly extraFacts = computed(() => {
    const b = this.bank();
    if (b) {
      return [
        { label: 'CLABE interbancaria', value: b.interbankCode || '—', mono: true },
        { label: 'Vencimiento', value: b.dateEnd ? formatDate(b.dateEnd) : 'Sin vencimiento', mono: true },
      ];
    }
    const i = this.insurance();
    if (!i) return [];
    return [
      { label: 'Beneficiario', value: i.beneficiaryName || 'Sin beneficiario designado', mono: false },
      { label: 'Vigencia hasta', value: i.dateEnd ? formatDate(i.dateEnd) : 'Sin vencimiento', mono: true },
    ];
  });

  protected readonly movementTotal = computed(() =>
    this.transactions().reduce((sum, t) => sum + (t.amount ?? 0), 0),
  );

  constructor() {
    effect(() => {
      const kind = this.area();
      const raw = this.id();
      const contractId = Number(raw);

      if (!raw || !Number.isInteger(contractId) || contractId <= 0 || (kind !== 'bank' && kind !== 'insurance')) {
        this.errorText.set('La dirección del contrato no es válida.');
        this.loading.set(false);
        return;
      }

      // El backend exige BancaOAdministrador / SegurosOAdministrador: si el rol no
      // los cumple, se explica en vez de disparar un 403 contra el servicio.
      const allowed = kind === 'bank' ? this.access.canBanca() : this.access.canSeguros();
      if (!allowed) {
        this.errorText.set(
          kind === 'bank'
            ? 'El detalle de contratos bancarios es del área de Banca.'
            : 'El detalle de pólizas es del área de Seguros.',
        );
        this.loading.set(false);
        return;
      }

      this.loading.set(true);
      this.errorText.set('');
      this.bank.set(null);
      this.insurance.set(null);
      this.operationsDenied.set(false);

      const detail$ = kind === 'bank'
        ? this.crm.getBankContract(contractId)
        : this.crm.getInsuranceContract(contractId);

      // Las operaciones son un panel más: si fallan, el contrato sigue leyéndose.
      const ops$ = kind === 'bank'
        ? this.crm.getTransactions(contractId).pipe(catchError(() => { this.operationsDenied.set(true); return of([]); }))
        : this.crm.getClaims(contractId).pipe(catchError(() => { this.operationsDenied.set(true); return of([]); }));

      forkJoin({ detail: detail$, ops: ops$ }).subscribe({
        next: ({ detail, ops }) => {
          if (kind === 'bank') {
            this.bank.set(detail as BankContractDetail);
            this.transactions.set(ops as TransactionResponse[]);
          } else {
            this.insurance.set(detail as InsuranceContractDetail);
            this.claims.set(ops as InsuranceClaimResponse[]);
          }
          this.loading.set(false);
        },
        error: (err) => {
          this.errorText.set(describeHttpError(err, 'No fue posible abrir el contrato.'));
          this.loading.set(false);
        },
      });
    });
  }

  protected statusClass(status: string): string {
    const s = (status ?? '').toLowerCase();
    if (s.includes('activ') || s.includes('vigente')) return 'badge badge-ok';
    if (s.includes('proceso') || s.includes('tramite') || s.includes('trámite')) return 'badge badge-warn';
    if (s.includes('cancel') || s.includes('rechaz') || s.includes('vencid')) return 'badge badge-down';
    return 'badge badge-off';
  }
}
