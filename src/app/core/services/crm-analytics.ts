import { ContractGrid, OportunityResponse } from '../../shared/models/api.model';
import { GroupedPoint, SeriesPoint, Slice } from '../../shared/models/chart.model';

/**
 * Todo lo que muestra el Panorama se deriva aquí, de las respuestas de la API.
 * Ninguna de estas funciones necesita el directorio de usuarios, que exige rol
 * Administrador: el panel tiene que servir a los 19 roles.
 */

const MONTH = new Intl.DateTimeFormat('es-MX', { month: 'short' });

function parseDate(raw: string | null | undefined): Date | null {
  if (!raw) return null;
  const d = new Date(raw);
  return Number.isNaN(d.getTime()) ? null : d;
}

function monthKey(d: Date): string {
  return `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}`;
}

function monthRange(months: number): string[] {
  const now = new Date();
  const keys: string[] = [];
  for (let i = months - 1; i >= 0; i--) {
    keys.push(monthKey(new Date(now.getFullYear(), now.getMonth() - i, 1)));
  }
  return keys;
}

function labelFor(key: string): string {
  const [year, month] = key.split('-').map(Number);
  return `${MONTH.format(new Date(year, month - 1, 1)).replace('.', '')} ${String(year).slice(2)}`;
}

/** Un contrato cuenta como vigente salvo que su estado diga lo contrario. */
const CLOSED_CONTRACT = ['cancelado', 'cerrado', 'vencido', 'liquidado', 'rechazado'];

export function isContractOpen(contract: ContractGrid): boolean {
  const status = (contract.contractStatusName ?? '').trim().toLowerCase();
  return !CLOSED_CONTRACT.some((c) => status.includes(c));
}

/** Etapas terminales del embudo: no cuentan como oportunidad viva. */
const CLOSED_STAGE = ['ganad', 'perdid', 'cerrad', 'cancelad'];

export function isOportunityOpen(o: OportunityResponse): boolean {
  const stage = (o.stageName ?? '').trim().toLowerCase();
  return !CLOSED_STAGE.some((s) => stage.includes(s));
}

export function isOportunityWon(o: OportunityResponse): boolean {
  return (o.stageName ?? '').trim().toLowerCase().includes('ganad');
}

/** Contratos abiertos acumulados al cierre de cada mes. */
export function contractsByMonth(contracts: ContractGrid[], months = 12): SeriesPoint[] {
  const dated = contracts
    .map((c) => parseDate(c.dateOpeningIssue))
    .filter((d): d is Date => d !== null)
    .sort((a, b) => a.getTime() - b.getTime());

  if (!dated.length) return [];

  return monthRange(months).map((key) => {
    const [year, month] = key.split('-').map(Number);
    const cutoff = new Date(year, month, 1).getTime();
    return {
      date: new Date(year, month - 1, 1).toISOString(),
      value: dated.filter((d) => d.getTime() < cutoff).length,
    };
  });
}

/**
 * Contratos por mes, separados en las dos áreas de negocio.
 *
 * El catálogo tiene tres áreas (General, Seguros, Banca). Un `else` metía
 * General en el cubo de Seguros y la gráfica mentía sobre su propia leyenda:
 * aquí solo se cuentan las dos áreas que la leyenda nombra.
 */
export function contractsByArea(contracts: ContractGrid[], months = 6): GroupedPoint[] {
  const keys = monthRange(months);
  const buckets = new Map(keys.map((k) => [k, { primary: 0, secondary: 0 }]));

  for (const contract of contracts) {
    const d = parseDate(contract.dateOpeningIssue);
    if (!d) continue;
    const bucket = buckets.get(monthKey(d));
    if (!bucket) continue;

    const area = (contract.areaName ?? '').trim().toLowerCase();
    if (area === 'banca') bucket.primary++;
    else if (area === 'seguros') bucket.secondary++;
    // Cualquier otra área queda fuera: no pertenece a esta comparación.
  }

  return keys.map((key) => ({
    label: labelFor(key),
    primary: buckets.get(key)!.primary,
    secondary: buckets.get(key)!.secondary,
  }));
}

/** Reparto por un campo de texto, mayor primero; los vacíos se agrupan. */
export function countBy<T>(rows: T[], pick: (row: T) => string | null | undefined, emptyLabel: string): Slice[] {
  const counts = new Map<string, number>();
  for (const row of rows) {
    const key = (pick(row) ?? '').trim() || emptyLabel;
    counts.set(key, (counts.get(key) ?? 0) + 1);
  }
  return [...counts.entries()]
    .map(([name, value]) => ({ name, value }))
    .sort((a, b) => b.value - a.value || a.name.localeCompare(b.name, 'es'));
}

/** Valor ponderado del embudo: monto × probabilidad de las oportunidades vivas. */
export function weightedPipeline(oportunities: OportunityResponse[]): number {
  return oportunities
    .filter(isOportunityOpen)
    .reduce((sum, o) => sum + (o.estimatedMont ?? 0) * ((o.succesProbability ?? 0) / 100), 0);
}

export function pipelineValue(oportunities: OportunityResponse[]): number {
  return oportunities.filter(isOportunityOpen).reduce((sum, o) => sum + (o.estimatedMont ?? 0), 0);
}

/** Oportunidades cuya fecha estimada de cierre ya pasó y siguen abiertas. */
export function overdueOportunities(oportunities: OportunityResponse[]): OportunityResponse[] {
  const today = Date.now();
  return oportunities.filter((o) => {
    if (!isOportunityOpen(o)) return false;
    const close = parseDate(o.dateEstimatedClose);
    return close !== null && close.getTime() < today;
  });
}

/** Las oportunidades que cierran antes, primero: es la cola de trabajo real. */
export function closingSoon(oportunities: OportunityResponse[], limit: number): OportunityResponse[] {
  return oportunities
    .filter((o) => isOportunityOpen(o) && parseDate(o.dateEstimatedClose) !== null)
    .sort((a, b) => parseDate(a.dateEstimatedClose)!.getTime() - parseDate(b.dateEstimatedClose)!.getTime())
    .slice(0, limit);
}

export function newestContracts(contracts: ContractGrid[], limit: number): ContractGrid[] {
  return [...contracts]
    .sort((a, b) => {
      const ta = parseDate(a.dateOpeningIssue)?.getTime() ?? -Infinity;
      const tb = parseDate(b.dateOpeningIssue)?.getTime() ?? -Infinity;
      return tb - ta;
    })
    .slice(0, limit);
}
