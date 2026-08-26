import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AuthService } from './auth.service';

/** Builds an unsigned JWT with the given payload — enough to exercise decoding. */
function makeToken(payload: Record<string, unknown>): string {
  const encode = (obj: unknown) =>
    btoa(String.fromCharCode(...new TextEncoder().encode(JSON.stringify(obj))))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode(payload)}.sig`;
}

describe('AuthService', () => {
  let service: AuthService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    service = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
    localStorage.clear();
  });

  it('starts logged out with no stored token', () => {
    expect(service.isLoggedIn()).toBe(false);
    expect(service.session()).toBeNull();
  });

  it('stores the token returned by login and reads identity from its claims', () => {
    service.login({ Email: 'ana@meridian.mx', Password: 'x' }).subscribe();

    const req = http.expectOne((r) => r.url.endsWith('/Auth/login'));
    expect(req.request.method).toBe('POST');
    req.flush({
      token: makeToken({
        name: 'Ana Herrera',
        email: 'ana@meridian.mx',
        role: 'Administrador',
        exp: Math.floor(Date.now() / 1000) + 3600,
      }),
      expirationInMinutes: 60,
    });

    expect(service.isLoggedIn()).toBe(true);
    expect(service.session()?.name).toBe('Ana Herrera');
    expect(service.session()?.role).toBe('Administrador');
  });

  it('treats an expired token as logged out', () => {
    service.setToken(makeToken({ email: 'a@b.mx', exp: Math.floor(Date.now() / 1000) - 60 }));
    expect(service.isLoggedIn()).toBe(false);
  });

  it('trusts a token with no exp claim', () => {
    service.setToken(makeToken({ email: 'a@b.mx' }));
    expect(service.isLoggedIn()).toBe(true);
  });

  it('survives a malformed token instead of throwing', () => {
    service.setToken('not-a-jwt');
    expect(service.session()).toBeNull();
    expect(service.isLoggedIn()).toBe(false);
  });

  it('clears the token on logout', () => {
    service.setToken(makeToken({ email: 'a@b.mx' }));
    service.logout();
    expect(service.getToken()).toBeNull();
    expect(service.isLoggedIn()).toBe(false);
  });
});
