import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { DomSanitizer, SafeHtml } from '@angular/platform-browser';
import { inject } from '@angular/core';

/**
 * One authored icon set: 24×24 grid, 1.6 stroke, round caps, no fills.
 * Every glyph in the product comes from here so stroke weight never drifts.
 */
const PATHS: Record<string, string> = {
  overview: '<path d="M4 13h5v7H4zM10 4h4v16h-4zM15 9h5v11h-5z"/>',
  users: '<circle cx="12" cy="8" r="3.4"/><path d="M5 20a7 7 0 0 1 14 0"/>',
  settings: '<circle cx="12" cy="12" r="3"/><path d="M12 3v2.2M12 18.8V21M21 12h-2.2M5.2 12H3M18.4 5.6l-1.6 1.6M7.2 16.8l-1.6 1.6M18.4 18.4l-1.6-1.6M7.2 7.2 5.6 5.6"/>',
  reports: '<path d="M6 3h8l4 4v14H6z"/><path d="M14 3v4h4"/><path d="M9 13h6M9 17h4"/>',
  search: '<circle cx="10.5" cy="10.5" r="6"/><path d="m15 15 4.5 4.5"/>',
  plus: '<path d="M12 5v14M5 12h14"/>',
  edit: '<path d="M4 20h4L19 9a2.1 2.1 0 0 0-3-3L5 17z"/><path d="m15 6 3 3"/>',
  power: '<path d="M12 4v7"/><path d="M7.4 7A7.5 7.5 0 1 0 16.6 7"/>',
  logout: '<path d="M14 4h4a1 1 0 0 1 1 1v14a1 1 0 0 1-1 1h-4"/><path d="M9 8l-4 4 4 4"/><path d="M5 12h10"/>',
  chevronLeft: '<path d="m14 6-6 6 6 6"/>',
  chevronRight: '<path d="m10 6 6 6-6 6"/>',
  chevronDown: '<path d="m6 9 6 6 6-6"/>',
  arrowUp: '<path d="M12 19V5"/><path d="m6 11 6-6 6 6"/>',
  arrowRight: '<path d="M5 12h14"/><path d="m13 6 6 6-6 6"/>',
  check: '<path d="m5 12.5 4.5 4.5L19 7"/>',
  close: '<path d="M6 6l12 12M18 6 6 18"/>',
  alert: '<path d="M12 4.5 21 20H3z"/><path d="M12 10v4"/><path d="M12 17h.01"/>',
  info: '<circle cx="12" cy="12" r="8.5"/><path d="M12 11v5"/><path d="M12 8h.01"/>',
  sun: '<circle cx="12" cy="12" r="4"/><path d="M12 2.5v2M12 19.5v2M21.5 12h-2M4.5 12h-2M18.4 5.6l-1.4 1.4M7 17l-1.4 1.4M18.4 18.4 17 17M7 7 5.6 5.6"/>',
  moon: '<path d="M20 14.5A8.2 8.2 0 0 1 9.5 4 8.5 8.5 0 1 0 20 14.5"/>',
  mail: '<rect x="3" y="5.5" width="18" height="13" rx="1.5"/><path d="m3.5 7 8.5 6 8.5-6"/>',
  shield: '<path d="M12 3.5 19 6v5.5c0 4.2-2.8 7.4-7 9-4.2-1.6-7-4.8-7-9V6z"/><path d="m9 12 2 2 4-4"/>',
  menu: '<path d="M4 7h16M4 12h16M4 17h16"/>',
  monitor: '<rect x="3" y="4.5" width="18" height="12.5" rx="1.5"/><path d="M9 20.5h6M12 17v3.5"/>',
  clients: '<circle cx="9" cy="8" r="3.2"/><path d="M3.5 19.5a5.8 5.8 0 0 1 11 0"/><path d="M16.5 6.4a3 3 0 0 1 0 5.7"/><path d="M17.6 14.6a5.4 5.4 0 0 1 3.4 4.9"/>',
  contracts: '<path d="M6 3h8l4 4v14H6z"/><path d="M14 3v4h4"/><path d="M9 12h6M9 16h4"/>',
  pipeline: '<path d="M3 5h18l-6.5 7.5V20l-5 1v-8.5z"/>',
  products: '<path d="M4 7.5 12 3.5l8 4v9L12 20.5l-8-4z"/><path d="m4 7.5 8 4 8-4M12 11.5v9"/>',
  building: '<path d="M4 20V6.5L12 3l8 3.5V20"/><path d="M9 20v-5h6v5"/><path d="M8.5 9.5h1M14.5 9.5h1M8.5 12.5h1M14.5 12.5h1"/>',
  person: '<circle cx="12" cy="8" r="3.4"/><path d="M5 20a7 7 0 0 1 14 0"/>',
  trash: '<path d="M4.5 7h15"/><path d="M9.5 7V5.2A1.2 1.2 0 0 1 10.7 4h2.6a1.2 1.2 0 0 1 1.2 1.2V7"/><path d="M6.5 7 7.4 20h9.2L17.5 7"/><path d="M10.5 11v5M13.5 11v5"/>',
  clock: '<circle cx="12" cy="12" r="8.5"/><path d="M12 7.5V12l3 1.8"/>',
};

@Component({
  selector: 'app-icon',
  standalone: true,
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <svg
      [attr.width]="size()"
      [attr.height]="size()"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      stroke-width="1.6"
      stroke-linecap="round"
      stroke-linejoin="round"
      aria-hidden="true"
      focusable="false"
      [innerHTML]="body()"
    ></svg>
  `,
  styles: [':host { display: inline-flex; }'],
})
export class Icon {
  private readonly sanitizer = inject(DomSanitizer);

  readonly name = input.required<string>();
  readonly size = input<number>(16);

  readonly body = computed<SafeHtml>(() =>
    // Paths are authored constants in this file, never user input.
    this.sanitizer.bypassSecurityTrustHtml(PATHS[this.name()] ?? ''),
  );
}
