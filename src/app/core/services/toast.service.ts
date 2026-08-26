import { Injectable, signal } from '@angular/core';

export type ToastKind = 'success' | 'error' | 'info';

export interface Toast {
  id: number;
  kind: ToastKind;
  text: string;
  leaving: boolean;
}

const LIFETIME_MS = 5000;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private nextId = 1;
  private readonly timers = new Map<number, ReturnType<typeof setTimeout>>();

  readonly toasts = signal<Toast[]>([]);

  success(text: string): void { this.push('success', text); }
  error(text: string): void { this.push('error', text); }
  info(text: string): void { this.push('info', text); }

  private push(kind: ToastKind, text: string): void {
    const id = this.nextId++;
    this.toasts.update((list) => [...list.slice(-3), { id, kind, text, leaving: false }]);
    this.timers.set(id, setTimeout(() => this.dismiss(id), LIFETIME_MS));
  }

  dismiss(id: number): void {
    const timer = this.timers.get(id);
    if (timer) {
      clearTimeout(timer);
      this.timers.delete(id);
    }
    // Mark leaving so the exit animation can run, then drop it.
    this.toasts.update((list) => list.map((t) => (t.id === id ? { ...t, leaving: true } : t)));
    setTimeout(() => this.toasts.update((list) => list.filter((t) => t.id !== id)), 160);
  }
}
