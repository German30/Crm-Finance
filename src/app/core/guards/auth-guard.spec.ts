import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import {
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  UrlTree,
  provideRouter,
} from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './auth-guard';
import { AuthService } from '../services/auth.service';

function token(payload: Record<string, unknown>): string {
  const encode = (obj: unknown) =>
    btoa(String.fromCharCode(...new TextEncoder().encode(JSON.stringify(obj))))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
  return `${encode({ alg: 'none' })}.${encode(payload)}.sig`;
}

describe('route guards', () => {
  let auth: AuthService;

  const run = (guard: typeof authGuard, url = '/admin/clients') =>
    TestBed.runInInjectionContext(() =>
      guard({} as ActivatedRouteSnapshot, { url } as RouterStateSnapshot),
    );

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    auth = TestBed.inject(AuthService);
  });

  afterEach(() => localStorage.clear());

  it('redirects an anonymous visitor to login, keeping the target', () => {
    const result = run(authGuard);
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toContain('/auth/login');
    expect((result as UrlTree).toString()).toContain('returnUrl');
  });

  it('lets an authenticated user through', () => {
    auth.setToken(token({ email: 'a@b.mx' }));
    expect(run(authGuard)).toBe(true);
  });

  it('clears an expired token instead of leaving it in storage', () => {
    auth.setToken(token({ email: 'a@b.mx', exp: Math.floor(Date.now() / 1000) - 10 }));
    run(authGuard);
    expect(auth.getToken()).toBeNull();
  });

  it('permissionGuard deja pasar al Administrador', () => {
    auth.setToken(token({ email: 'a@b.mx', role: 'Administrador', Area: 'General' }));
    expect(run(permissionGuard('users'))).toBe(true);
  });

  it('permissionGuard desvía a un rol sin permiso y explica por qué', () => {
    auth.setToken(token({ email: 'a@b.mx', role: 'Cajero / Operador de Ventanilla', Area: 'Banca' }));
    const result = run(permissionGuard('users'));
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toContain('/admin/overview');
  });

  it('permissionGuard respeta el área del rol', () => {
    auth.setToken(token({ email: 'a@b.mx', role: 'Subgerente de Sucursal', Area: 'Banca' }));
    expect(run(permissionGuard('banca'))).toBe(true);
    expect(run(permissionGuard('seguros')) instanceof UrlTree).toBe(true);
  });

  it('keeps an authenticated user off the login screen', () => {
    auth.setToken(token({ email: 'a@b.mx' }));
    const result = TestBed.runInInjectionContext(() =>
      guestGuard({} as ActivatedRouteSnapshot, { url: '/auth/login' } as RouterStateSnapshot),
    );
    expect(result instanceof UrlTree).toBe(true);
    expect((result as UrlTree).toString()).toContain('/admin/overview');
  });
});
