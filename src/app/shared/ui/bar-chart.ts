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
import { GroupedPoint } from '../models/chart.model';
import { countTicks, formatNumber } from '../utils/format';

/**
 * Two counts on one shared axis — the same unit, so they belong on the same
 * scale. Grouped bars with a 2px surface gap; per-bar hover tooltip.
 */
@Component({
  selector: 'app-bar-chart',
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
      >
        @for (tick of ticks(); track tick.y) {
          <line class="grid-line" [attr.x1]="pad.l" [attr.x2]="width() - pad.r" [attr.y1]="tick.y" [attr.y2]="tick.y" />
          <text class="tick-num" [attr.x]="pad.l - 8" [attr.y]="tick.y + 3.5" text-anchor="end">{{ tick.label }}</text>
        }

        @for (bar of bars(); track bar.key) {
          <rect
            [attr.x]="bar.x"
            [attr.y]="bar.y"
            [attr.width]="bar.w"
            [attr.height]="bar.h"
            [attr.fill]="bar.color"
            rx="3"
            (pointerenter)="active.set(bar.key)"
            (pointerleave)="active.set(null)"
            [attr.opacity]="active() === null || active() === bar.key ? 1 : 0.45"
          >
            <title>{{ bar.title }}</title>
          </rect>
        }

        @for (label of groupLabels(); track label.x) {
          <text [attr.x]="label.x" [attr.y]="height() - 6" text-anchor="middle">{{ label.text }}</text>
        }
      </svg>

      @if (activeBar(); as bar) {
        <div class="chart-tip" [style.left.px]="bar.x + bar.w / 2" [style.top.px]="bar.y - 8">
          <span class="tip-label">{{ bar.month }}</span>
          <div class="tip-row">
            <span class="k">
              <span class="legend-swatch" [style.background]="bar.color"></span>
              {{ bar.series }}
            </span>
            <span class="v">{{ bar.amount }}</span>
          </div>
        </div>
      }
    </div>
  `,
})
export class BarChart {
  readonly data = input.required<GroupedPoint[]>();
  readonly primaryLabel = input('Serie A');
  readonly secondaryLabel = input('Serie B');
  readonly height = input(230);

  protected readonly pad = { t: 12, r: 8, b: 24, l: 56 };

  private readonly wrap = viewChild.required<ElementRef<HTMLElement>>('wrap');
  protected readonly width = signal(560);
  protected readonly active = signal<string | null>(null);

  constructor() {
    const observer = new ResizeObserver((entries) => {
      const next = Math.max(280, Math.round(entries[0].contentRect.width));
      if (next !== this.width()) this.width.set(next);
    });
    queueMicrotask(() => observer.observe(this.wrap().nativeElement));
    inject(DestroyRef).onDestroy(() => observer.disconnect());
  }

  private readonly scale = computed(() => {
    const data = this.data();
    if (!data.length) return null;
    const peak = Math.max(...data.flatMap((d) => [d.primary, d.secondary]));
    // A flat-zero month still needs an axis to sit against.
    const max = Math.max(1, Math.ceil(peak * 1.15));
    const innerH = Math.max(1, this.height() - this.pad.t - this.pad.b);
    return { data, max, innerH, y: (v: number) => this.pad.t + innerH - (v / max) * innerH };
  });

  protected readonly bars = computed(() => {
    const s = this.scale();
    if (!s) return [];
    const innerW = Math.max(1, this.width() - this.pad.l - this.pad.r);
    const groupW = innerW / s.data.length;
    const barW = Math.max(6, Math.min(26, (groupW - 14) / 2 - 1));

    return s.data.flatMap((point, i) => {
      const centre = this.pad.l + groupW * i + groupW / 2;
      // 2px of surface between the pair — the gap is the separator, not a stroke.
      const left = centre - barW - 1;
      const series = [
        { name: this.primaryLabel(), value: point.primary, color: 'var(--series-1)', x: left },
        { name: this.secondaryLabel(), value: point.secondary, color: 'var(--series-2)', x: centre + 1 },
      ];
      return series.map((entry) => {
        const y = s.y(entry.value);
        return {
          key: `${point.label}-${entry.name}`,
          month: point.label,
          series: entry.name,
          color: entry.color,
          amount: formatNumber(entry.value),
          title: `${point.label} · ${entry.name}: ${formatNumber(entry.value)}`,
          x: entry.x,
          y,
          w: barW,
          // A zero count draws no bar at all; a 2px stub would read as one.
          h: entry.value === 0 ? 0 : Math.max(2, this.height() - this.pad.b - y),
        };
      });
    });
  });

  protected readonly activeBar = computed(() => {
    const key = this.active();
    return key ? (this.bars().find((b) => b.key === key) ?? null) : null;
  });

  protected readonly ticks = computed(() => {
    const s = this.scale();
    if (!s) return [];
    return countTicks(s.max, 4).map((value) => ({
      y: Math.round(s.y(value)) + 0.5,
      label: formatNumber(value),
    }));
  });

  protected readonly groupLabels = computed(() => {
    const s = this.scale();
    if (!s) return [];
    const innerW = Math.max(1, this.width() - this.pad.l - this.pad.r);
    const groupW = innerW / s.data.length;
    return s.data.map((p, i) => ({ x: this.pad.l + groupW * i + groupW / 2, text: p.label }));
  });

  protected readonly ariaLabel = computed(() => {
    const data = this.data();
    if (!data.length) return 'Sin datos para graficar';
    return (
      `${this.primaryLabel()} y ${this.secondaryLabel()} por mes. ` +
      data
        .map(
          (d) =>
            `${d.label}: ${this.primaryLabel()} ${formatNumber(d.primary)}, ` +
            `${this.secondaryLabel()} ${formatNumber(d.secondary)}`,
        )
        .join('. ')
    );
  });
}
