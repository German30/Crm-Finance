import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { AccessService } from '../services/access.service';
import { ToastService } from '../services/toast.service';

/** Bloquea el área privada y recuerda a dónde iba el usuario. */
export const authGuard: CanActivateFn = (_route, state) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  if (auth.isLoggedIn()) return true;

  // Un token caducado sigue en storage a estas alturas: hay que limpiarlo.
  auth.logout();
  return router.createUrlTree(['/auth/login'], {
    queryParams: state.url && state.url !== '/' ? { returnUrl: state.url } : undefined,
  });
};

/** Mantiene fuera del login a quien ya tiene sesión. */
export const guestGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);
  return auth.isLoggedIn() ? router.createUrlTree(['/admin/overview']) : true;
};

/**
 * Guard por permiso. El backend ya responde 403; esto evita que el usuario
 * aterrice en una pantalla que solo puede fallar, y le dice por qué.
 */
export function permissionGuard(
  permission: 'users' | 'products' | 'banca' | 'seguros',
): CanActivateFn {
  return () => {
    const access = inject(AccessService);
    const router = inject(Router);
    const toast = inject(ToastService);

    const allowed = {
      users: access.canManageUsers(),
      products: access.canManageProducts(),
      banca: access.canBanca(),
      seguros: access.canSeguros(),
    }[permission];

    if (allowed) return true;

    const reason = {
      users: 'La gestión de usuarios está reservada al rol Administrador.',
      products: 'Dar de alta o editar productos está reservado al rol Administrador.',
      banca: 'Esa sección es del área de Banca.',
      seguros: 'Esa sección es del área de Seguros.',
    }[permission];

    toast.info(`${reason} Tu rol es ${access.role() || 'desconocido'}.`);
    return router.createUrlTree(['/admin/overview']);
  };
}
