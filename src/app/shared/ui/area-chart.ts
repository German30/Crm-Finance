import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  ElementRef,
  computed,
  inject,
  input,
  signal,
  viewChild,
} from '@angular/core';
import { SeriesPoint } from '../models/chart.model';
import { formatMonthYear, formatNumber, niceTicks } from '../utils/format';

interface Tick {
  y: number;
  label: string;
}

/**
 * Single-series value-over-time. One series, so no legend box — the panel title
 * names it. Crosshair + tooltip ship by default; the chart is interactive.
 */
@Component({
  selector: 'app-area-chart',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="chart-wrap" #wrap>
      <svg
        class="chart"
        [attr.width]="width()"
        [attr.height]="height()"
        [attr.viewBox]="'0 0 ' + width() + ' ' + height()"
        role="img"
        [attr.aria-label]="ariaLabel()"
        (pointermove)="onMove($event)"
        (pointerleave)="active.set(null)"
      >
        <defs>
          <linearGradient [attr.id]="gradientId" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stop-color="var(--accent)" stop-opacity="0.26" />
            <stop offset="100%" stop-color="var(--accent)" stop-opacity="0" />
          </linearGradient>
        </defs>

        @for (tick of ticks(); track tick.y) {
          <line class="grid-line" [attr.x1]="pad.l" [attr.x2]="width() - pad.r" [attr.y1]="tick.y" [attr.y2]="tick.y" />
          <text class="tick-num" [attr.x]="pad.l - 8" [attr.y]="tick.y + 3.5" text-anchor="end">{{ tick.label }}</text>
        }

        @for (label of xLabels(); track label.x) {
          <text [attr.x]="label.x" [attr.y]="height() - 6" text-anchor="middle">{{ label.text }}</text>
        }

        @if (areaPath()) {
          <path [attr.d]="areaPath()" [attr.fill]="'url(#' + gradientId + ')'" />
          <path
            [attr.d]="linePath()"
            fill="none"
            stroke="var(--accent)"
            stroke-width="2"
            stroke-linejoin="round"
            stroke-linecap="round"
          />
        }

        @if (activePoint(); as p) {
          <line
            class="axis-line"
            [attr.x1]="p.x" [attr.x2]="p.x"
            [attr.y1]="pad.t" [attr.y2]="height() - pad.b"
            stroke="var(--line-strong)"
          />
          <circle [attr.cx]="p.x" [attr.cy]="p.y" r="5" fill="var(--accent)" stroke="var(--bg-surface)" stroke-width="2" />
        }
      </svg>

      @if (activePoint(); as p) {
        <div class="chart-tip" [style.left.px]="p.x" [style.top.px]="p.y - 12">
          <span class="tip-label">{{ p.label }}</span>
          <div class="tip-row">
            <span class="k">{{ valueLabel() }}</span>
            <span class="v">{{ p.value }}</span>
          </div>
        </div>
      }
    </div>
  `,
})
export class AreaChart {
  readonly points = input.required<SeriesPoint[]>();
  /** What the value means, so ticks and the tooltip read correctly. */
  readonly valueLabel = input('Total');
  readonly height = input(268);

  protected readonly pad = { t: 14, r: 10, b: 24, l: 56 };
  protected readonly gradientId = `area-fill-${Math.random().toString(36).slice(2, 8)}`;

  private readonly wrap = viewChild.required<ElementRef<HTMLElement>>('wrap');
  protected readonly width = signal(720);
  protected readonly active = signal<number | null>(null);

  constructor() {
    const observer = new ResizeObserver((entries) => {
      const next = Math.max(280, Math.round(entries[0].contentRect.width));
      if (next !== this.width()) this.width.set(next);
    });
    // Observe after the view exists; the signal keeps the first frame sensible.
    queueMicrotask(() => observer.observe(this.wrap().nativeElement));
    inject(DestroyRef).onDestroy(() => observer.disconnect());
  }

  private readonly scale = computed(() => {
    const data = this.points();
    const w = this.width();
    const h = this.height();
    if (!data.length) return null;

    const values = data.map((d) => d.value);
    const rawMin = Math.min(...values);
    const rawMax = Math.max(...values);
    const span = rawMax - rawMin || Math.max(rawMax, 1);
    // Counts never go negative, so the lower pad stops at zero.
    const min = Math.max(0, rawMin - span * 0.15);
    const max = rawMax + span * 0.15;

    const innerW = Math.max(1, w - this.pad.l - this.pad.r);
    const innerH = Math.max(1, h - this.pad.t - this.pad.b);
    const x = (i: number) =>
      this.pad.l + (data.length === 1 ? innerW / 2 : (i / (data.length - 1)) * innerW);
    const y = (v: number) => this.pad.t + innerH - ((v - min) / (max - min)) * innerH;

    return { data, x, y, min, max };
  });

  protected readonly linePath = computed(() => {
    const s = this.scale();
    if (!s) return '';
    return s.data.map((d, i) => `${i ? 'L' : 'M'}${s.x(i).toFixed(1)} ${s.y(d.value).toFixed(1)}`).join(' ');
  });

  protected readonly areaPath = computed(() => {
    const s = this.scale();
    if (!s) return '';
    const base = this.height() - this.pad.b;
    return `${this.linePath()} L${s.x(s.data.length - 1).toFixed(1)} ${base} L${s.x(0).toFixed(1)} ${base} Z`;
  });

  protected readonly ticks = computed<Tick[]>(() => {
    const s = this.scale();
    if (!s) return [];
    // Round values only — a counting axis must never print the same label twice.
    return niceTicks(s.min, s.max, 4).map((value) => ({
      y: Math.round(s.y(value)) + 0.5,
      label: formatNumber(value),
    }));
  });

  protected readonly xLabels = computed(() => {
    const s = this.scale();
    if (!s) return [];
    // Roughly one label per 110px, never more than the data supports.
    const slots = Math.max(2, Math.min(6, Math.floor((this.width() - this.pad.l) / 110)));
    const step = Math.max(1, Math.floor((s.data.length - 1) / slots));
    const out: { x: number; text: string }[] = [];
    for (let i = 0; i < s.data.length; i += step) {
      out.push({ x: s.x(i), text: formatMonthYear(s.data[i].date) });
    }
    return out;
  });

  protected readonly activePoint = computed(() => {
    const s = this.scale();
    const i = this.active();
    if (!s || i === null || !s.data[i]) return null;
    const d = s.data[i];
    return {
      x: s.x(i),
      y: s.y(d.value),
      label: formatMonthYear(d.date),
      value: formatNumber(d.value),
    };
  });

  protected readonly ariaLabel = computed(() => {
    const data = this.points();
    if (!data.length) return 'Sin datos para graficar';
    const first = data[0];
    const last = data[data.length - 1];
    return `${this.valueLabel()} de ${formatMonthYear(first.date)} a ${formatMonthYear(last.date)}: de ${formatNumber(first.value)} a ${formatNumber(last.value)}.`;
  });

  protected onMove(event: PointerEvent): void {
    const s = this.scale();
    if (!s) return;
    const rect = (event.currentTarget as SVGSVGElement).getBoundingClientRect();
    const innerW = Math.max(1, this.width() - this.pad.l - this.pad.r);
    const ratio = (event.clientX - rect.left - this.pad.l) / innerW;
    const index = Math.round(ratio * (s.data.length - 1));
    this.active.set(Math.max(0, Math.min(s.data.length - 1, index)));
  }
}
