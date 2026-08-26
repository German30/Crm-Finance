import { Injectable, effect, signal } from '@angular/core';

export type ThemeChoice = 'dark' | 'light' | 'system';

const KEY = 'meridian.theme';

function readStored(): ThemeChoice {
  try {
    const raw = localStorage.getItem(KEY);
    return raw === 'dark' || raw === 'light' ? raw : 'system';
  } catch {
    return 'system';
  }
}

@Injectable({ providedIn: 'root' })
export class ThemeService {
  readonly choice = signal<ThemeChoice>(readStored());

  constructor() {
    effect(() => {
      const choice = this.choice();
      const root = document.documentElement;
      if (choice === 'system') root.removeAttribute('data-theme');
      else root.setAttribute('data-theme', choice);
      try {
        if (choice === 'system') localStorage.removeItem(KEY);
        else localStorage.setItem(KEY, choice);
      } catch {
        /* preference simply will not persist */
      }
    });
  }

  /** True when the rendered result is dark, whatever produced it. */
  isDark(): boolean {
    const choice = this.choice();
    if (choice !== 'system') return choice === 'dark';
    return !window.matchMedia('(prefers-color-scheme: light)').matches;
  }

  toggle(): void {
    this.choice.set(this.isDark() ? 'light' : 'dark');
  }
}
