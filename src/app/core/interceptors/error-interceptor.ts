import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

/** Human-readable text for a failed request, in the product's own language. */
export function describeHttpError(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) return fallback;

  // ProblemDetails / ModelState from ASP.NET Core, when present.
  const body = error.error as
    | { title?: string; detail?: string; message?: string; errors?: Record<string, string[]> }
    | string
    | null;

  if (typeof body === 'string' && body.trim() && !body.trim().startsWith('<')) {
    return body.trim();
  }
  if (body && typeof body === 'object') {
    if (body.errors) {
      const first = Object.values(body.errors).flat().filter(Boolean)[0];
      if (first) return first;
    }
    const named = body.detail || body.message || body.title;
    if (named) return named;
  }

  switch (error.status) {
    case 0:
      return 'No hay conexión con el servidor. Verifica que la API esté disponible.';
    case 400:
      return 'Los datos enviados no son válidos. Revisa el formulario.';
    case 403:
      return 'Tu cuenta no tiene permisos para esta operación.';
    case 404:
      return 'El registro solicitado ya no existe.';
    case 409:
      return 'El registro entra en conflicto con uno existente.';
    case 422:
      return 'No fue posible procesar los datos enviados.';
    default:
      return error.status >= 500
        ? 'El servidor respondió con un error. Intenta de nuevo en unos momentos.'
        : fallback;
  }
}

/**
 * Turns an expired or rejected session into a clean logout instead of letting
 * every screen fail on its own.
 */
export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      const isLoginCall = req.url.includes('/Auth/login');
      if (error.status === 401 && !isLoginCall && auth.getToken()) {
        toast.info('Tu sesión expiró. Inicia sesión de nuevo.');
        auth.logoutAndRedirect(router.url);
      }
      return throwError(() => error);
    }),
  );
};
