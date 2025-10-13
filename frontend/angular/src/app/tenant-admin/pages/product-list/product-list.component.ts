import { AsyncPipe, CurrencyPipe, DatePipe, NgIf } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Observable, Subject, startWith, switchMap } from 'rxjs';
import {
  TenantAdminProductService,
  TenantProductSummary
} from '../../services/tenant-admin-product.service';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [
    AsyncPipe,
    CurrencyPipe,
    DatePipe,
    NgIf,
    RouterLink,
    MatTableModule,
    MatButtonModule,
    MatIconModule,
    MatTooltipModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  template: `
    <section class="product-list">
      <header>
        <div>
          <h1>Products</h1>
          <p class="subtitle">Manage all products visible in your storefront catalogue.</p>
        </div>
        <div class="header-actions">
          <button mat-raised-button color="primary" routerLink="/tenant-admin/products/new">
            <mat-icon>add</mat-icon>
            New product
          </button>
          <button mat-icon-button matTooltip="Refresh" (click)="refresh()">
            <mat-icon>refresh</mat-icon>
          </button>
        </div>
      </header>

      <ng-container *ngIf="products$ | async as products; else loading">
        <mat-table [dataSource]="products" class="mat-elevation-z1">
          <ng-container matColumnDef="name">
            <mat-header-cell *matHeaderCellDef>Name</mat-header-cell>
            <mat-cell *matCellDef="let product">
              <div class="cell-primary">{{ product.name }}</div>
              <div class="cell-secondary">SKU {{ product.sku }}</div>
            </mat-cell>
          </ng-container>

          <ng-container matColumnDef="price">
            <mat-header-cell *matHeaderCellDef>Price</mat-header-cell>
            <mat-cell *matCellDef="let product">{{ product.price | currency: product.currency }}</mat-cell>
          </ng-container>

          <ng-container matColumnDef="status">
            <mat-header-cell *matHeaderCellDef>Status</mat-header-cell>
            <mat-cell *matCellDef="let product">
              <mat-chip [color]="product.published ? 'primary' : undefined" selected>
                {{ product.published ? 'Published' : 'Draft' }}
              </mat-chip>
            </mat-cell>
          </ng-container>

          <ng-container matColumnDef="updatedAt">
            <mat-header-cell *matHeaderCellDef>Last updated</mat-header-cell>
            <mat-cell *matCellDef="let product">{{ product.updatedAt | date: 'medium' }}</mat-cell>
          </ng-container>

          <ng-container matColumnDef="actions">
            <mat-header-cell *matHeaderCellDef class="align-right">Actions</mat-header-cell>
            <mat-cell *matCellDef="let product" class="align-right">
              <a mat-stroked-button color="primary" [routerLink]="['/tenant-admin/products', product.id]">
                <mat-icon>edit</mat-icon>
                Edit
              </a>
            </mat-cell>
          </ng-container>

          <mat-header-row *matHeaderRowDef="displayedColumns"></mat-header-row>
          <mat-row *matRowDef="let row; columns: displayedColumns"></mat-row>
        </mat-table>

        <div *ngIf="products.length === 0" class="empty">
          <mat-icon>inventory_2</mat-icon>
          <p>No products yet. Use the "New product" button to add your first item.</p>
        </div>
      </ng-container>

      <ng-template #loading>
        <div class="loading">
          <mat-progress-spinner mode="indeterminate"></mat-progress-spinner>
        </div>
      </ng-template>
    </section>
  `,
  styles: [
    `
      .product-list {
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
      }

      header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        flex-wrap: wrap;
      }

      .subtitle {
        color: rgba(0, 0, 0, 0.54);
        margin: 0;
      }

      .header-actions {
        display: flex;
        align-items: center;
        gap: 0.5rem;
      }

      mat-table {
        width: 100%;
      }

      mat-header-cell,
      mat-cell {
        padding: 1rem;
      }

      .cell-primary {
        font-weight: 600;
      }

      .cell-secondary {
        color: rgba(0, 0, 0, 0.54);
        font-size: 0.85rem;
      }

      .align-right {
        justify-content: flex-end;
      }

      .empty {
        display: grid;
        justify-items: center;
        gap: 0.75rem;
        padding: 3rem 1rem;
        color: rgba(0, 0, 0, 0.6);
      }

      .empty mat-icon {
        font-size: 3rem;
        height: 3rem;
        width: 3rem;
      }

      .loading {
        display: flex;
        justify-content: center;
        padding: 3rem 0;
      }
    `
  ]
})
export class ProductListComponent {
  private readonly productService = inject(TenantAdminProductService);
  private readonly reload$ = new Subject<void>();

  protected readonly displayedColumns = ['name', 'price', 'status', 'updatedAt', 'actions'];
  protected readonly products$: Observable<TenantProductSummary[]> = this.reload$.pipe(
    startWith(void 0),
    switchMap(() => this.productService.getProducts())
  );

  refresh(): void {
    this.reload$.next();
  }
}
