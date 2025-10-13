import { Component, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Observable } from 'rxjs';
import { AdminTenantService, TenantSummary } from '../../services/admin-tenant.service';

@Component({
  selector: 'app-tenants-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterLink,
    DatePipe,
    MatTableModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  template: `
    <section class="tenants-list">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Tenants</mat-card-title>
          <mat-card-subtitle>Monitor and manage all provisioned tenants.</mat-card-subtitle>
        </mat-card-header>

        <mat-card-content>
          <ng-container *ngIf="tenants$ | async as tenants; else loading">
            <table mat-table [dataSource]="tenants" class="mat-elevation-z1">
              <ng-container matColumnDef="name">
                <th mat-header-cell *matHeaderCellDef>Tenant</th>
                <td mat-cell *matCellDef="let tenant">
                  <div class="tenant-primary">
                    <span class="name">{{ tenant.name }}</span>
                    <a [routerLink]="['/admin/tenants', tenant.id]" mat-button color="primary">View</a>
                  </div>
                </td>
              </ng-container>

              <ng-container matColumnDef="domain">
                <th mat-header-cell *matHeaderCellDef>Domain</th>
                <td mat-cell *matCellDef="let tenant">
                  <div class="domain">
                    <span>{{ tenant.subdomain }}.{{ tenant.rootDomain }}</span>
                    <span *ngIf="tenant.customDomain" class="custom-domain">{{ tenant.customDomain }}</span>
                  </div>
                </td>
              </ng-container>

              <ng-container matColumnDef="plan">
                <th mat-header-cell *matHeaderCellDef>Plan</th>
                <td mat-cell *matCellDef="let tenant">{{ tenant.plan }}</td>
              </ng-container>

              <ng-container matColumnDef="status">
                <th mat-header-cell *matHeaderCellDef>Status</th>
                <td mat-cell *matCellDef="let tenant">
                  <mat-chip [color]="tenant.status === 'active' ? 'primary' : 'warn'" selected>
                    {{ tenant.status | titlecase }}
                  </mat-chip>
                </td>
              </ng-container>

              <ng-container matColumnDef="created">
                <th mat-header-cell *matHeaderCellDef>Created</th>
                <td mat-cell *matCellDef="let tenant">{{ tenant.createdAt | date: 'mediumDate' }}</td>
              </ng-container>

              <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
              <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
            </table>

            <div class="empty" *ngIf="!tenants.length">
              <p>No tenants have been provisioned yet.</p>
              <a mat-raised-button color="primary" routerLink="/admin/tenants/create">Create your first tenant</a>
            </div>
          </ng-container>
        </mat-card-content>
      </mat-card>
    </section>

    <ng-template #loading>
      <div class="loading">
        <mat-progress-spinner mode="indeterminate"></mat-progress-spinner>
        <p>Loading tenants…</p>
      </div>
    </ng-template>
  `,
  styles: [
    `
      .tenants-list mat-card {
        padding: 1rem 1.5rem 2rem;
      }

      table {
        width: 100%;
      }

      th.mat-mdc-header-cell {
        font-weight: 600;
      }

      .tenant-primary {
        display: flex;
        align-items: center;
        gap: 0.75rem;
      }

      .tenant-primary .name {
        font-weight: 600;
      }

      .domain {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
      }

      .custom-domain {
        font-size: 0.85rem;
        color: rgba(0, 0, 0, 0.6);
      }

      .empty {
        text-align: center;
        padding: 2rem 0;
      }

      .loading {
        display: flex;
        flex-direction: column;
        align-items: center;
        gap: 1rem;
        padding: 2rem 0;
      }
    `
  ]
})
export class TenantsListComponent {
  private readonly tenantService = inject(AdminTenantService);

  protected readonly tenants$: Observable<TenantSummary[]> = this.tenantService.getTenants();
  protected readonly displayedColumns = ['name', 'domain', 'plan', 'status', 'created'];
}
