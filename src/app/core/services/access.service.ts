import { Injectable, computed, inject } from '@angular/core';
import { AuthService } from './auth.service';

/**
 * Espejo en el cliente de las políticas de Program.cs.
 *
 *   RequiereAdministrador  -> rol "Administrador"
 *   AreaBanca / AreaSeguros-> claim Area
 *   BancaOAdministrador    -> Administrador OR Area=Banca
 *   SegurosOAdministrador  -> Administrador OR Area=Seguros
 *
 * Esto NO es seguridad: el backend sigue siendo la autoridad y responde 403.
 * Sirve para no ofrecer al usuario acciones que su rol tiene prohibidas —
 * un botón que siempre falla es peor que un botón ausente.
 */

export const ADMIN_ROLE = 'Administrador';
export const AREA_BANCA = 'Banca';
export const AREA_SEGUROS = 'Seguros';
export const AREA_GENERAL = 'General';

@Injectable({ providedIn: 'root' })
export class AccessService {
  private readonly auth = inject(AuthService);

  readonly role = computed(() => this.auth.session()?.role ?? '');
  readonly area = computed(() => this.auth.session()?.area ?? '');

  readonly isAdmin = computed(() => this.role() === ADMIN_ROLE);
  readonly isBanca = computed(() => this.area() === AREA_BANCA);
  readonly isSeguros = computed(() => this.area() === AREA_SEGUROS);

  /** Política BancaOAdministrador. */
  readonly canBanca = computed(() => this.isAdmin() || this.isBanca());

  /** Política SegurosOAdministrador. */
  readonly canSeguros = computed(() => this.isAdmin() || this.isSeguros());

  /** GET/POST/PUT /api/User y sus acciones: RequiereAdministrador. */
  readonly canManageUsers = this.isAdmin;

  /** POST/PUT/DELETE /api/Product: RequiereAdministrador. Leer, cualquiera. */
  readonly canManageProducts = this.isAdmin;

  /** DELETE de cliente, contrato, transacción y siniestro: RequiereAdministrador. */
  readonly canDelete = this.isAdmin;

  /** Clientes, contratos (rejilla), oportunidades y catálogos: cualquier autenticado. */
  readonly canReadBusiness = computed(() => !!this.auth.session());

  /**
   * Qué área mostrar por defecto en los filtros. Un usuario de Banca no debería
   * aterrizar viendo pólizas que no puede abrir.
   */
  readonly defaultAreaFilter = computed(() => {
    if (this.isAdmin()) return '';
    return this.area() === AREA_GENERAL ? '' : this.area();
  });

  /** Etiqueta corta del alcance, para explicarle al usuario qué está viendo. */
  readonly scopeLabel = computed(() => {
    if (this.isAdmin()) return 'Acceso total';
    const area = this.area();
    return area && area !== AREA_GENERAL ? `Área ${area}` : 'Acceso general';
  });
}
