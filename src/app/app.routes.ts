import { Routes } from '@angular/router';
import { authGuard, guestGuard, permissionGuard } from './core/guards/auth-guard';

export const routes: Routes = [
  {
    path: 'auth/login',
    canActivate: [guestGuard],
    title: 'Iniciar sesión · Meridian',
    loadComponent: () => import('./modules/auth/login/login').then((m) => m.Login),
  },
  {
    path: 'admin',
    canActivate: [authGuard],
    loadComponent: () => import('./modules/admin/shell/shell').then((m) => m.Shell),
    children: [
      {
        path: 'overview',
        title: 'Panorama · Meridian',
        loadComponent: () => import('./modules/admin/overview/overview').then((m) => m.Overview),
      },
      {
        path: 'clients',
        title: 'Clientes · Meridian',
        loadComponent: () => import('./modules/admin/clients/client-list').then((m) => m.ClientList),
      },
      {
        path: 'clients/:type/:id',
        title: 'Expediente de cliente · Meridian',
        loadComponent: () =>
          import('./modules/admin/clients/client-detail').then((m) => m.ClientDetail),
      },
      {
        path: 'contracts',
        title: 'Contratos · Meridian',
        loadComponent: () =>
          import('./modules/admin/contracts/contract-list').then((m) => m.ContractList),
      },
      {
        path: 'contracts/:area/:id',
        title: 'Detalle de contrato · Meridian',
        loadComponent: () =>
          import('./modules/admin/contracts/contract-detail').then((m) => m.ContractDetail),
      },
      {
        path: 'pipeline',
        title: 'Oportunidades · Meridian',
        loadComponent: () => import('./modules/admin/pipeline/pipeline').then((m) => m.Pipeline),
      },
      {
        path: 'products',
        title: 'Productos · Meridian',
        loadComponent: () =>
          import('./modules/admin/products/product-list').then((m) => m.ProductList),
      },
      // Gestión de usuarios: política RequiereAdministrador en el backend.
      {
        path: 'users',
        canActivate: [permissionGuard('users')],
        title: 'Usuarios · Meridian',
        loadComponent: () => import('./modules/admin/user-list/user-list').then((m) => m.UserList),
      },
      {
        path: 'users/new',
        canActivate: [permissionGuard('users')],
        title: 'Nuevo usuario · Meridian',
        loadComponent: () => import('./modules/admin/user-form/user-form').then((m) => m.UserForm),
      },
      {
        path: 'users/edit/:id',
        canActivate: [permissionGuard('users')],
        title: 'Editar usuario · Meridian',
        loadComponent: () => import('./modules/admin/user-form/user-form').then((m) => m.UserForm),
      },
      {
        path: 'settings',
        title: 'Ajustes · Meridian',
        loadComponent: () => import('./modules/admin/settings/settings').then((m) => m.Settings),
      },
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
    ],
  },
  { path: '', redirectTo: 'admin/overview', pathMatch: 'full' },
  {
    path: '**',
    title: 'Página no encontrada · Meridian',
    loadComponent: () => import('./modules/not-found/not-found').then((m) => m.NotFound),
  },
];
