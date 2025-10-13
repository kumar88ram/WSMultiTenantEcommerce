import { Component, inject } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import { Observable } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatTooltipModule } from '@angular/material/tooltip';
import { AdminAnalyticsOverview, AdminAnalyticsService } from '../../services/admin-analytics.service';

@Component({
  selector: 'app-analytics',
  standalone: true,
  imports: [
    CommonModule,
    DatePipe,
    MatCardModule,
    MatProgressBarModule,
    MatDividerModule,
    MatListModule,
    MatTooltipModule
  ],
  template: `
    <section class="analytics" *ngIf="overview$ | async as overview">
      <header>
        <h1>Platform analytics</h1>
        <p>Track health and performance across all tenants.</p>
      </header>

      <div class="kpi-grid">
        <mat-card *ngFor="let metric of overview.kpis">
          <mat-card-title>{{ metric.label }}</mat-card-title>
          <mat-card-content>
            <div class="kpi-value">{{ metric.value }}</div>
            <p class="muted">{{ metric.deltaLabel }}</p>
          </mat-card-content>
        </mat-card>
      </div>

      <div class="chart-grid">
        <mat-card class="span-2">
          <mat-card-header>
            <mat-card-title>Revenue trend</mat-card-title>
            <mat-card-subtitle>Last {{ overview.revenueTrend.length }} weeks</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="bar-chart">
              <div
                *ngFor="let point of overview.revenueTrend"
                class="bar"
                [style.--value]="point.value"
                [matTooltip]="point.label + ': ' + (point.value | currency)"
              >
                <span>{{ point.value | number: '1.0-0' }}</span>
              </div>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header>
            <mat-card-title>Active tenants</mat-card-title>
            <mat-card-subtitle>{{ overview.activeTenants }} / {{ overview.totalTenants }}</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <mat-progress-bar mode="determinate" [value]="overview.activeTenantsRatio * 100"></mat-progress-bar>
            <p class="muted">{{ overview.activeTenantsRatio * 100 | number: '1.0-1' }}% active</p>
            <mat-divider></mat-divider>
            <mat-list>
              <mat-list-item *ngFor="let segment of overview.tenantsByPlan">
                <div matListItemTitle>{{ segment.plan }}</div>
                <div matListItemLine>{{ segment.count }} tenants</div>
                <span matListItemMeta>{{ segment.percentage | number: '1.0-1' }}%</span>
              </mat-list-item>
            </mat-list>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header>
            <mat-card-title>Top tenants by revenue</mat-card-title>
            <mat-card-subtitle>Last 30 days</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <mat-list>
              <mat-list-item *ngFor="let tenant of overview.topTenants">
                <div matListItemTitle>{{ tenant.name }}</div>
                <div matListItemLine>{{ tenant.category }}</div>
                <span matListItemMeta>{{ tenant.revenue | currency }}</span>
              </mat-list-item>
            </mat-list>
          </mat-card-content>
        </mat-card>
      </div>
    </section>
  `,
  styles: [
    `
      header {
        margin-bottom: 2rem;
      }

      .kpi-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 1rem;
        margin-bottom: 2rem;
      }

      .kpi-value {
        font-size: 2rem;
        font-weight: 600;
      }

      .muted {
        color: rgba(0, 0, 0, 0.54);
      }

      .chart-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: 1.5rem;
      }

      .span-2 {
        grid-column: span 2;
      }

      .bar-chart {
        display: grid;
        grid-auto-flow: column;
        align-items: end;
        gap: 0.75rem;
        min-height: 200px;
      }

      .bar {
        display: grid;
        align-items: end;
        justify-items: center;
        background: rgba(25, 118, 210, 0.2);
        border-radius: 0.75rem 0.75rem 0 0;
        height: calc(var(--value) * 1px);
        min-height: 40px;
        position: relative;
        padding: 0.5rem;
        color: rgba(0, 0, 0, 0.7);
        font-weight: 600;
      }

      .bar span {
        transform: rotate(-90deg);
        font-size: 0.75rem;
        opacity: 0.7;
      }

      mat-progress-bar {
        margin: 1rem 0;
      }

      @media (max-width: 960px) {
        .span-2 {
          grid-column: span 1;
        }
      }
    `
  ]
})
export class AnalyticsComponent {
  private readonly analyticsService = inject(AdminAnalyticsService);
  protected readonly overview$: Observable<AdminAnalyticsOverview> = this.analyticsService.getOverview();
}
