import { Routes } from '@angular/router';
import { provideAuthGuard } from './core/guards/auth.guard';

export const appRoutes: Routes = [
  {
    path: 'login',
    loadComponent: () => import('./auth/components/login/login.component').then(m => m.LoginComponent)
  },
  {
    path: 'admin',
    canActivate: [provideAuthGuard()],
    loadChildren: () => import('./admin/admin.routes').then(m => m.ADMIN_ROUTES)
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
  }
];
