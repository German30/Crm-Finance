import { Injectable, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { ChangePasswordRequest, LoginRequest, LoginResponse } from '../../shared/models/api.model';
import { SessionUser } from '../../shared/models/session.model';

const TOKEN_KEY = 'meridian.token';

/** localStorage throws in private modes and is absent during SSR/prerender. */
function readStorage(key: string): string | null {
  try {
    return typeof localStorage === 'undefined' ? null : localStorage.getItem(key);
  } catch {
    return null;
  }
}

function writeStorage(key: string, value: string | null): void {
  try {
    if (typeof localStorage === 'undefined') return;
    if (value === null) localStorage.removeItem(key);
    else localStorage.setItem(key, value);
  } catch {
    /* storage unavailable — the session simply does not survive a reload */
  }
}

/**
 * Decodifica el payload de un JWT. Devuelve null solo si es realmente ilegible.
 *
 * El backend firma nombres de rol con acentos ("Especialista en Suscripción",
 * "Coordinador / Analista de Riesgo y Crédito"): 6 de los 19 roles del catálogo.
 * El payload viaja en UTF-8, así que hay que decodificarlo como UTF-8 — con
 * `decodeURIComponent` sobre bytes crudos, cualquier secuencia inesperada lanza
 * y dejaría a esos roles sin poder entrar.
 */
function decodeJwt(token: string): Record<string, unknown> | null {
  const parts = token.split('.');
  if (parts.length !== 3) return null;

  try {
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const padded = base64.padEnd(base64.length + ((4 - (base64.length % 4)) % 4), '=');
    const binary = atob(padded);

    const bytes = new Uint8Array(binary.length);
    for (let i = 0; i < binary.length; i++) bytes[i] = binary.charCodeAt(i);

    // `fatal: false` sustituye un byte inválido por U+FFFD en vez de lanzar:
    // un acento mal codificado no debe costarle la sesión al usuario.
    const json =
      typeof TextDecoder !== 'undefined'
        ? new TextDecoder('utf-8', { fatal: false }).decode(bytes)
        : binary;

    return JSON.parse(json);
  } catch {
    return null;
  }
}

const CLAIM = {
  name: [
    'name',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name',
    'unique_name',
    'given_name',
  ],
  email: [
    'email',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress',
  ],
  role: [
    'role',
    'http://schemas.microsoft.com/ws/2008/06/identity/claims/role',
  ],
  id: [
    'nameid',
    'sub',
    'http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier',
  ],
  // Claim propio del backend: decide qué módulos de negocio puede abrir el usuario.
  area: ['Area'],
} as const;

function pick(payload: Record<string, unknown>, keys: readonly string[]): string | null {
  for (const key of keys) {
    const raw = payload[key];
    if (Array.isArray(raw) && raw.length) return String(raw[0]);
    if (typeof raw === 'string' && raw.trim()) return raw;
    if (typeof raw === 'number') return String(raw);
  }
  return null;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly apiUrl = `${environment.apiUrl}/Auth`;

  private readonly tokenSignal = signal<string | null>(readStorage(TOKEN_KEY));

  readonly token = this.tokenSignal.asReadonly();

  /** The decoded payload, or null when the token is absent or unreadable. */
  private readonly payload = computed(() => {
    const token = this.tokenSignal();
    return token ? decodeJwt(token) : null;
  });

  /** Identity read off the JWT — no extra request, no stale copy in storage. */
  readonly session = computed<SessionUser | null>(() => {
    const payload = this.payload();
    if (!payload) return null;
    const email = pick(payload, CLAIM.email) ?? '';
    const rawId = pick(payload, CLAIM.id);
    const parsedId = rawId !== null ? Number(rawId) : NaN;
    const exp = typeof payload['exp'] === 'number' ? (payload['exp'] as number) * 1000 : null;
    return {
      name: pick(payload, CLAIM.name) ?? email.split('@')[0] ?? 'Usuario',
      email,
      role: pick(payload, CLAIM.role) ?? '',
      area: pick(payload, CLAIM.area) ?? '',
      userId: Number.isFinite(parsedId) ? parsedId : null,
      expiresAt: exp,
    };
  });

  readonly isLoggedIn = computed(() => {
    // The API issues JWTs, so anything that will not decode is corrupt —
    // whatever put it in storage, it is not a session.
    const session = this.session();
    if (!session) return false;

    // A JWT with no exp claim is trusted until the API rejects it.
    return session.expiresAt == null || session.expiresAt > Date.now();
  });

  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.apiUrl}/login`, credentials).pipe(
      tap((response) => {
        if (response?.token) this.setToken(response.token);
      }),
    );
  }

  setToken(token: string): void {
    writeStorage(TOKEN_KEY, token);
    this.tokenSignal.set(token);
  }

  getToken(): string | null {
    return this.tokenSignal();
  }

  logout(): void {
    writeStorage(TOKEN_KEY, null);
    this.tokenSignal.set(null);
  }

  /** Clears the session and sends the user to the login screen. */
  /** POST /api/Auth/change-password — el usuario cambia la suya propia. */
  changePassword(body: ChangePasswordRequest): Observable<unknown> {
    return this.http.post(`${this.apiUrl}/change-password`, body);
  }

  logoutAndRedirect(returnUrl?: string): void {
    this.logout();
    this.router.navigate(['/auth/login'], {
      queryParams: returnUrl && returnUrl !== '/' ? { returnUrl } : undefined,
    });
  }
}
