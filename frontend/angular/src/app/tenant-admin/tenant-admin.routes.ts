import { Routes } from '@angular/router';
import { provideTenantAdminGuard } from './guards/tenant-admin.guard';

export const TENANT_ADMIN_ROUTES: Routes = [
  {
    path: '',
    canActivate: [provideTenantAdminGuard()],
    loadComponent: () =>
      import('./layout/tenant-admin-layout.component').then(m => m.TenantAdminLayoutComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'products' },
      {
        path: 'products',
        loadComponent: () =>
          import('./pages/product-list/product-list.component').then(m => m.ProductListComponent)
      },
      {
        path: 'products/new',
        loadComponent: () =>
          import('./pages/product-edit/product-edit.component').then(m => m.ProductEditComponent)
      },
      {
        path: 'products/:productId',
        loadComponent: () =>
          import('./pages/product-edit/product-edit.component').then(m => m.ProductEditComponent)
      },
      {
        path: 'menus',
        loadComponent: () =>
          import('./pages/menu-builder/menu-builder.component').then(m => m.MenuBuilderComponent)
      },
      {
        path: 'forms',
        loadComponent: () =>
          import('./pages/form-builder/form-builder.component').then(m => m.FormBuilderComponent)
      },
      {
        path: 'settings',
        loadComponent: () =>
          import('./pages/settings/settings.component').then(m => m.SettingsComponent)
      },
      {
        path: 'theme/customize',
        loadComponent: () =>
          import('./pages/theme-customizer/theme-customizer.component').then(m => m.ThemeCustomizerComponent)
      }
    ]
  }
];
