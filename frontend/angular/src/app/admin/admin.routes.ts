import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () => import('./layout/admin-layout.component').then(m => m.AdminLayoutComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'tenants' },
      {
        path: 'tenants',
        loadComponent: () =>
          import('./pages/tenants-list/tenants-list.component').then(m => m.TenantsListComponent)
      },
      {
        path: 'tenants/create',
        loadComponent: () =>
          import('./pages/tenant-create/tenant-create.component').then(m => m.TenantCreateComponent)
      },
      {
        path: 'tenants/:tenantId',
        loadComponent: () =>
          import('./pages/tenant-detail/tenant-detail.component').then(m => m.TenantDetailComponent)
      },
      {
        path: 'plans',
        loadComponent: () => import('./pages/plans/plans.component').then(m => m.PlansComponent)
      },
      {
        path: 'orders',
        loadComponent: () => import('./pages/orders/order-list.component').then(m => m.OrderListComponent)
      },
      {
        path: 'analytics',
        loadComponent: () =>
          import('./pages/analytics/analytics.component').then(m => m.AnalyticsComponent)
      },
      {
        path: 'plugins',
        loadComponent: () =>
          import('./pages/plugins/plugins.component').then(m => m.PluginsComponent)
      },
      {
        path: 'themes/analytics',
        loadComponent: () =>
          import('./pages/theme-management/theme-analytics.component').then(m => m.ThemeAnalyticsComponent)
      },
      {
        path: 'themes',
        loadComponent: () =>
          import('./pages/theme-management/theme-list.component').then(m => m.ThemeListComponent)
      }
    ]
  }
];
