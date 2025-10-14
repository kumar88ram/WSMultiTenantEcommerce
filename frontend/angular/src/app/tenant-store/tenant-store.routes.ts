import { Routes } from '@angular/router';

export const TENANT_STORE_ROUTES: Routes = [
  {
    path: ':tenant',
    children: [
      {
        path: '',
        pathMatch: 'full',
        redirectTo: 'products'
      },
      {
        path: 'products',
        children: [
          {
            path: '',
            loadComponent: () =>
              import('./components/product-list/product-list.component').then(m => m.ProductListComponent)
          },
          {
            path: ':productSlug',
            loadComponent: () =>
              import('./components/product-detail/product-detail.component').then(m => m.ProductDetailComponent)
          }
        ]
      }
    ]
  }
];
