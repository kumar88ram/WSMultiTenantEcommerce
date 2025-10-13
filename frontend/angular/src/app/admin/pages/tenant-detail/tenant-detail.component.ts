import { Component, inject } from '@angular/core';
import { CommonModule, DatePipe, NgForOf, NgIf } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs/operators';
import { Observable } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatDividerModule } from '@angular/material/divider';
import { MatIconModule } from '@angular/material/icon';
import { AdminTenantService, TenantDetail } from '../../services/admin-tenant.service';

@Component({
  selector: 'app-tenant-detail',
  standalone: true,
  imports: [
    CommonModule,
    NgIf,
    NgForOf,
    RouterLink,
    DatePipe,
    MatCardModule,
    MatChipsModule,
    MatButtonModule,
    MatListModule,
    MatDividerModule,
    MatIconModule
  ],
  template: `
    <ng-container *ngIf="tenant$ | async as tenant; else loading">
      <div class="tenant-detail">
        <header class="tenant-header">
          <div>
            <h1>{{ tenant.name }}</h1>
            <p class="muted">Tenant ID: {{ tenant.id }}</p>
            <div class="chips">
              <mat-chip color="primary" selected>{{ tenant.status | titlecase }}</mat-chip>
              <mat-chip>{{ tenant.plan }}</mat-chip>
            </div>
          </div>
          <div class="actions">
            <a mat-stroked-button color="primary" routerLink="/admin/tenants">Back to tenants</a>
            <button mat-flat-button color="primary">Impersonate admin</button>
          </div>
        </header>

        <section class="layout">
          <mat-card class="span-2">
            <mat-card-header>
              <mat-card-title>Domain configuration</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <dl>
                <div>
                  <dt>Primary domain</dt>
                  <dd>{{ tenant.subdomain }}.{{ tenant.rootDomain }}</dd>
                </div>
                <div>
                  <dt>Custom domain</dt>
                  <dd>{{ tenant.customDomain || 'Not configured' }}</dd>
                </div>
                <div>
                  <dt>Created</dt>
                  <dd>{{ tenant.createdAt | date: 'medium' }}</dd>
                </div>
                <div>
                  <dt>Owner</dt>
                  <dd>{{ tenant.ownerName }} · {{ tenant.ownerEmail }}</dd>
                </div>
              </dl>
            </mat-card-content>
          </mat-card>

          <mat-card>
            <mat-card-header>
              <mat-card-title>Subscription</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <p><strong>Plan:</strong> {{ tenant.plan }}</p>
              <p><strong>Renewal date:</strong> {{ tenant.renewalDate | date: 'mediumDate' }}</p>
              <p><strong>Mrr:</strong> {{ tenant.mrr | currency }}</p>
              <button mat-button color="primary">Change plan</button>
            </mat-card-content>
          </mat-card>

          <mat-card>
            <mat-card-header>
              <mat-card-title>Usage metrics</mat-card-title>
            </mat-card-header>
            <mat-card-content class="usage">
              <div>
                <span class="label">Orders (30d)</span>
                <span class="value">{{ tenant.metrics.orders }}</span>
              </div>
              <div>
                <span class="label">GMV (30d)</span>
                <span class="value">{{ tenant.metrics.revenue | currency }}</span>
              </div>
              <div>
                <span class="label">Active users</span>
                <span class="value">{{ tenant.metrics.activeUsers }}</span>
              </div>
            </mat-card-content>
          </mat-card>

          <mat-card class="span-2">
            <mat-card-header>
              <mat-card-title>Integrations</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <mat-list>
                <mat-list-item *ngFor="let integration of tenant.integrations">
                  <mat-icon matListItemIcon>{{ integration.icon }}</mat-icon>
                  <div matListItemTitle>{{ integration.name }}</div>
                  <div matListItemLine>{{ integration.description }}</div>
                  <mat-chip [color]="integration.status === 'active' ? 'primary' : 'warn'" selected>
                    {{ integration.status | titlecase }}
                  </mat-chip>
                </mat-list-item>
              </mat-list>
            </mat-card-content>
          </mat-card>
        </section>
      </div>
    </ng-container>

    <ng-template #loading>
      <div class="loading">
        <mat-icon>hourglass_empty</mat-icon>
        <p>Loading tenant details…</p>
      </div>
    </ng-template>
  `,
  styles: [
    `
      .tenant-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-start;
        margin-bottom: 2rem;
        gap: 2rem;
      }

      .chips {
        display: flex;
        gap: 0.75rem;
        margin-top: 1rem;
      }

      .muted {
        color: rgba(0, 0, 0, 0.54);
      }

      .layout {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: 1.5rem;
      }

      .span-2 {
        grid-column: span 2;
      }

      dl {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: 1rem 2rem;
        margin: 0;
      }

      dt {
        font-weight: 600;
      }

      dd {
        margin: 0;
      }

      .usage {
        display: grid;
        gap: 1rem;
      }

      .usage .label {
        color: rgba(0, 0, 0, 0.54);
      }

      .usage .value {
        font-size: 1.35rem;
        font-weight: 600;
      }

      .loading {
        display: grid;
        justify-items: center;
        gap: 1rem;
        color: rgba(0, 0, 0, 0.54);
      }

      @media (max-width: 960px) {
        .tenant-header {
          flex-direction: column;
          align-items: stretch;
        }

        .span-2 {
          grid-column: span 1;
        }
      }
    `
  ]
})
export class TenantDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly tenantService = inject(AdminTenantService);

  protected readonly tenant$: Observable<TenantDetail> = this.route.paramMap.pipe(
    switchMap(params => {
      const tenantId = params.get('tenantId') ?? '';
      return this.tenantService.getTenant(tenantId);
    })
  );
}
