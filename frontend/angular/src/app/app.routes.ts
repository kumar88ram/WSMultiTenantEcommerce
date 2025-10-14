import { Routes } from '@angular/router';
import { provideAuthGuard } from './core/guards/auth.guard';
import { provideTenantAdminGuard } from './tenant-admin/guards/tenant-admin.guard';

export const appRoutes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./auth/components/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'admin',
    canActivate: [provideAuthGuard()],
    loadChildren: () => import('./admin/admin.module').then(m => m.AdminModule)
  },
  {
    path: 'tenant-admin',
    canActivate: [provideAuthGuard(), provideTenantAdminGuard()],
    loadChildren: () => import('./tenant-admin/tenant-admin.module').then(m => m.TenantAdminModule)
  },
  {
    path: 'store',
    loadChildren: () => import('./tenant-store/tenant-store.routes').then(m => m.TENANT_STORE_ROUTES)
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
  }
];
