import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { AccessService } from './access.service';
import { AuthService } from './auth.service';

/** Un JWT sin firmar con los claims que emite AuthService.cs. */
function token(role: string, area: string): string {
  const enc = (o: unknown) =>
    // UTF-8 primero, igual que el backend: los roles llevan acentos.
    btoa(String.fromCharCode(...new TextEncoder().encode(JSON.stringify(o))))
      .replace(/\+/g, '-')
      .replace(/\//g, '_')
      .replace(/=+$/, '');
  return [
    enc({ alg: 'none' }),
    enc({
      nameid: '7',
      name: 'Persona de prueba',
      email: 'persona@meridian.mx',
      role,
      Area: area,
      exp: Math.floor(Date.now() / 1000) + 3600,
    }),
    'sig',
  ].join('.');
}

/** Los 19 roles del catálogo, con el área a la que cuelgan en la BD. */
const CATALOGO: { role: string; area: string }[] = [
  { role: 'Administrador', area: 'General' },
  { role: 'Director o Gerente de Sucursal (Seguros)', area: 'Seguros' },
  { role: 'Coordinador Administrativo / RRHH (Seguros)', area: 'Seguros' },
  { role: 'Auxiliar Administrativo / Recepción (Seguros)', area: 'Seguros' },
  { role: 'Líder Comercial / Gerente de Ventas (Seguros)', area: 'Seguros' },
  { role: 'Ejecutivo de Cuenta o Asesor (Seguros)', area: 'Seguros' },
  { role: 'Agente de Seguros / Corredor', area: 'Seguros' },
  { role: 'Especialista en Suscripción', area: 'Seguros' },
  { role: 'Gestor de Siniestros / Reclamos', area: 'Seguros' },
  { role: 'Ejecutivo de Atención al Cliente (Seguros)', area: 'Seguros' },
  { role: 'Gerente de Sucursal / Director de Agencia', area: 'Banca' },
  { role: 'Subgerente de Sucursal', area: 'Banca' },
  { role: 'Ejecutivo de Atención al Cliente (Banca)', area: 'Banca' },
  { role: 'Asesor Financiero / Ejecutivo Pyme', area: 'Banca' },
  { role: 'Ejecutivo Hipotecarios', area: 'Banca' },
  { role: 'Cajero Principal / Jefe de Caja', area: 'Banca' },
  { role: 'Cajero / Operador de Ventanilla', area: 'Banca' },
  { role: 'Coordinador / Analista de Riesgo y Crédito', area: 'Banca' },
  { role: 'Asistente Administrativo / Recepcionista (Banca)', area: 'Banca' },
];

describe('AccessService', () => {
  let access: AccessService;
  let auth: AuthService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])],
    });
    auth = TestBed.inject(AuthService);
    access = TestBed.inject(AccessService);
  });

  afterEach(() => localStorage.clear());

  const signIn = (role: string, area: string) => auth.setToken(token(role, area));

  it('sin sesión no concede nada', () => {
    expect(access.isAdmin()).toBe(false);
    expect(access.canBanca()).toBe(false);
    expect(access.canSeguros()).toBe(false);
    expect(access.canManageUsers()).toBe(false);
    expect(access.canReadBusiness()).toBe(false);
  });

  it('el Administrador cumple todas las políticas', () => {
    signIn('Administrador', 'General');
    expect(access.isAdmin()).toBe(true);
    expect(access.canBanca()).toBe(true);
    expect(access.canSeguros()).toBe(true);
    expect(access.canManageUsers()).toBe(true);
    expect(access.canManageProducts()).toBe(true);
    expect(access.canDelete()).toBe(true);
  });

  it('un rol de Banca abre Banca pero no Seguros', () => {
    signIn('Cajero / Operador de Ventanilla', 'Banca');
    expect(access.canBanca()).toBe(true);
    expect(access.canSeguros()).toBe(false);
  });

  it('un rol de Seguros abre Seguros pero no Banca', () => {
    signIn('Gestor de Siniestros / Reclamos', 'Seguros');
    expect(access.canSeguros()).toBe(true);
    expect(access.canBanca()).toBe(false);
  });

  it('ningún rol distinto de Administrador gestiona usuarios', () => {
    for (const { role, area } of CATALOGO.filter((r) => r.role !== 'Administrador')) {
      signIn(role, area);
      expect(access.canManageUsers())
        .withContext(`${role} no debería gestionar usuarios`)
        .toBe(false);
      expect(access.canManageProducts())
        .withContext(`${role} no debería administrar productos`)
        .toBe(false);
      expect(access.canDelete()).withContext(`${role} no debería borrar`).toBe(false);
    }
  });

  it('los 19 roles del catálogo pueden leer el negocio', () => {
    for (const { role, area } of CATALOGO) {
      signIn(role, area);
      expect(access.canReadBusiness()).withContext(`${role} debería entrar`).toBe(true);
      // Y cada uno alcanza exactamente una de las dos áreas, salvo el administrador.
      const alcance = [access.canBanca(), access.canSeguros()].filter(Boolean).length;
      expect(alcance).withContext(`${role} con áreas: ${alcance}`).toBe(role === 'Administrador' ? 2 : 1);
    }
  });

  it('el filtro de área por defecto respeta el área del rol', () => {
    signIn('Administrador', 'General');
    expect(access.defaultAreaFilter()).toBe('');

    signIn('Subgerente de Sucursal', 'Banca');
    expect(access.defaultAreaFilter()).toBe('Banca');

    signIn('Agente de Seguros / Corredor', 'Seguros');
    expect(access.defaultAreaFilter()).toBe('Seguros');
  });

  it('describe el alcance en palabras', () => {
    signIn('Administrador', 'General');
    expect(access.scopeLabel()).toBe('Acceso total');

    signIn('Ejecutivo Hipotecarios', 'Banca');
    expect(access.scopeLabel()).toBe('Área Banca');
  });

  it('un token sin claim de área no abre ningún área', () => {
    auth.setToken(token('Cajero / Operador de Ventanilla', ''));
    expect(access.canBanca()).toBe(false);
    expect(access.canSeguros()).toBe(false);
    // Pero sigue siendo una sesión válida para lo que no depende del área.
    expect(access.canReadBusiness()).toBe(true);
  });
});
