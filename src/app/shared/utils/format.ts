const LOCALE = 'es-MX';

const plain = new Intl.NumberFormat(LOCALE, { maximumFractionDigits: 0 });

const money = new Intl.NumberFormat(LOCALE, {
  style: 'currency',
  currency: 'MXN',
  maximumFractionDigits: 0,
});

const fullDate = new Intl.DateTimeFormat(LOCALE, { day: '2-digit', month: 'short', year: 'numeric' });
const timeOnly = new Intl.DateTimeFormat(LOCALE, { hour: '2-digit', minute: '2-digit' });
const monthYear = new Intl.DateTimeFormat(LOCALE, { month: 'short', year: '2-digit' });

export function formatMoney(value: number | null | undefined): string {
  return typeof value === 'number' && Number.isFinite(value) ? money.format(value) : '—';
}

/** 12.4 M / 840 k — para ejes y cintas, donde el ancho escasea. */
export function formatCompact(value: number | null | undefined): string {
  if (typeof value !== 'number' || !Number.isFinite(value)) return '—';
  const abs = Math.abs(value);
  if (abs >= 1_000_000_000) return (value / 1_000_000_000).toFixed(2) + ' MMM';
  if (abs >= 1_000_000) return (value / 1_000_000).toFixed(abs >= 10_000_000 ? 1 : 2) + ' M';
  if (abs >= 1_000) return Math.round(value / 1_000) + ' k';
  return plain.format(value);
}

export function formatNumber(value: number): string {
  return Number.isFinite(value) ? plain.format(value) : '—';
}

function toDate(value: string | Date | null | undefined): Date | null {
  if (!value) return null;
  const d = value instanceof Date ? value : new Date(value);
  return Number.isNaN(d.getTime()) ? null : d;
}

export function formatDate(value: string | Date | null | undefined): string {
  const d = toDate(value);
  return d ? fullDate.format(d) : '—';
}

export function formatTime(value: string | Date | null | undefined): string {
  const d = toDate(value);
  return d ? timeOnly.format(d) : '—';
}

/** "ago 26" — the axis label for a month bucket. */
export function formatMonthYear(value: string | Date | null | undefined): string {
  const d = toDate(value);
  return d ? monthYear.format(d).replace('.', '') : '—';
}

/** "hace 3 días" — relative age, for last-contact style columns. */
export function formatAgo(value: string | Date | null | undefined): string {
  const d = toDate(value);
  if (!d) return '—';
  const days = Math.floor((Date.now() - d.getTime()) / 86_400_000);
  if (days <= 0) return 'hoy';
  if (days === 1) return 'ayer';
  if (days < 30) return `hace ${days} días`;
  const months = Math.floor(days / 30);
  if (months < 12) return `hace ${months} ${months === 1 ? 'mes' : 'meses'}`;
  const years = Math.floor(days / 365);
  return `hace ${years} ${years === 1 ? 'año' : 'años'}`;
}

/** Two-letter monogram for the avatar, accent-safe. */
export function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (!parts.length) return '—';
  if (parts.length === 1) return parts[0].slice(0, 2);
  return parts[0][0] + parts[parts.length - 1][0];
}

/** Accent- and case-insensitive contains, for the list filters. */
export function fold(value: string): string {
  return value
    .toLowerCase()
    .normalize('NFD')
    .split('')
    .filter((ch) => {
      const code = ch.charCodeAt(0);
      return code < 0x0300 || code > 0x036f;
    })
    .join('');
}

/**
 * Axis ticks on round numbers. Counting axes must not print "3" twice because
 * two fractional steps rounded to the same integer, so when the whole range is
 * small the step is forced to a whole number.
 */
export function niceTicks(min: number, max: number, target = 4): number[] {
  if (!Number.isFinite(min) || !Number.isFinite(max)) return [];
  if (max === min) return [min];

  const rawStep = (max - min) / target;
  const magnitude = Math.pow(10, Math.floor(Math.log10(rawStep)));
  const normalized = rawStep / magnitude;
  // {1, 2, 2.5, 5, 10}: without 2.5 a 0–100 axis at target 4 steps by 50 and
  // yields three ticks instead of five.
  const niceUnit =
    normalized <= 1 ? 1
    : normalized <= 2 ? 2
    : normalized <= 2.5 ? 2.5
    : normalized <= 5 ? 5
    : 10;
  const step = Math.max(niceUnit * magnitude, 1e-9);

  const start = Math.ceil(min / step) * step;
  const out: number[] = [];
  for (let v = start; v <= max + step * 1e-6; v += step) {
    // Kill floating-point dust like 0.30000000000000004.
    out.push(Math.round(v / step) * step);
  }
  return out;
}

/** Whole-number ticks for a count axis, never repeating a label. */
export function countTicks(max: number, target = 4): number[] {
  const top = Math.max(1, Math.ceil(max));
  const step = Math.max(1, Math.ceil(top / target));
  const out: number[] = [];
  for (let v = 0; v <= top; v += step) out.push(v);
  return out;
}
