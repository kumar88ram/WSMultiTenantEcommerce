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
    loadChildren: () => import('./admin/admin.module').then(m => m.AdminModule)
  },
  {
    path: '',
    pathMatch: 'full',
    redirectTo: 'login'
  }
];
