import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatTooltipModule } from '@angular/material/tooltip';
import { NgChartsModule } from 'ng2-charts';
import { ChartConfiguration } from 'chart.js';
import { ThemeService } from '../../../core/services/theme.service';
import { ThemeAnalyticsSummary } from '../../../core/models/theme.models';
import { ThemeUsageDialogComponent } from './theme-usage-dialog.component';

@Component({
  selector: 'app-theme-analytics',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatTableModule,
    MatIconModule,
    MatTooltipModule,
    MatProgressBarModule,
    MatSnackBarModule,
    MatDialogModule,
    NgChartsModule,
    ThemeUsageDialogComponent
  ],
  template: `
    <section class="analytics">
      <mat-card>
        <mat-card-header>
          <mat-card-title>Theme Usage Analytics</mat-card-title>
          <mat-card-subtitle>Active tenants and adoption trends</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <mat-progress-bar *ngIf="loading()" mode="indeterminate"></mat-progress-bar>
          <div class="chart-wrapper" *ngIf="!loading() && chartConfig()?.data.datasets[0].data.length">
            <canvas baseChart [data]="chartConfig()?.data" [options]="chartConfig()?.options" [type]="chartConfig()?.type"></canvas>
          </div>
          <p *ngIf="!loading() && !analytics().length">No analytics data available yet.</p>
        </mat-card-content>
        <mat-card-actions align="end">
          <button mat-stroked-button (click)="loadAnalytics()">Refresh</button>
        </mat-card-actions>
      </mat-card>

      <mat-card>
        <mat-card-header>
          <mat-card-title>Theme Adoption by Tenant</mat-card-title>
        </mat-card-header>
        <mat-card-content>
          <table mat-table [dataSource]="analytics()" class="theme-table">
            <ng-container matColumnDef="name">
              <th mat-header-cell *matHeaderCellDef>Theme Name</th>
              <td mat-cell *matCellDef="let row">{{ row.themeName }}</td>
            </ng-container>
            <ng-container matColumnDef="active">
              <th mat-header-cell *matHeaderCellDef>Active Tenants</th>
              <td mat-cell *matCellDef="let row">{{ row.activeTenantsCount }}</td>
            </ng-container>
            <ng-container matColumnDef="avg">
              <th mat-header-cell *matHeaderCellDef>Avg Active Days</th>
              <td mat-cell *matCellDef="let row">{{ row.averageActiveDays | number: '1.0-1' }}</td>
            </ng-container>
            <ng-container matColumnDef="top">
              <th mat-header-cell *matHeaderCellDef>Top Tenants</th>
              <td mat-cell *matCellDef="let row">
                <span *ngIf="row.topTenants?.length; else noTenants">
                  {{ row.topTenants.slice(0, 3).map(t => t.tenantId).join(', ') }}
                </span>
                <ng-template #noTenants>—</ng-template>
              </td>
            </ng-container>
            <ng-container matColumnDef="actions">
              <th mat-header-cell *matHeaderCellDef>Actions</th>
              <td mat-cell *matCellDef="let row">
                <button mat-icon-button matTooltip="View tenants" (click)="viewTenants(row)">
                  <mat-icon>groups</mat-icon>
                </button>
              </td>
            </ng-container>

            <tr mat-header-row *matHeaderRowDef="displayedColumns"></tr>
            <tr mat-row *matRowDef="let row; columns: displayedColumns"></tr>
          </table>
        </mat-card-content>
      </mat-card>
    </section>
  `,
  styles: [
    `
      .analytics {
        display: grid;
        gap: 2rem;
        padding: 1rem;
      }
      .chart-wrapper {
        position: relative;
        width: 100%;
        max-width: 720px;
        margin: 0 auto;
      }
      .theme-table {
        width: 100%;
      }
      mat-card + mat-card {
        margin-top: 1rem;
      }
    `
  ]
})
export class ThemeAnalyticsComponent implements OnInit {
  private readonly themeService = inject(ThemeService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialog = inject(MatDialog);

  protected readonly analytics = signal<ThemeAnalyticsSummary[]>([]);
  protected readonly loading = signal(false);
  protected readonly displayedColumns = ['name', 'active', 'avg', 'top', 'actions'];

  protected readonly chartConfig = computed<ChartConfiguration<'bar'>>(() => {
    const summaries = this.analytics();
    return {
      type: 'bar',
      data: {
        labels: summaries.map(s => s.themeName || 'Untitled'),
        datasets: [
          {
            label: 'Active Tenants',
            data: summaries.map(s => s.activeTenantsCount),
            backgroundColor: '#3f51b5'
          }
        ]
      },
      options: {
        responsive: true,
        plugins: {
          legend: { display: false }
        }
      }
    };
  });

  ngOnInit(): void {
    this.loadAnalytics();
  }

  loadAnalytics(): void {
    this.loading.set(true);
    this.themeService.getThemeAnalytics().subscribe({
      next: response => {
        this.analytics.set(response);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load theme analytics', 'Dismiss', { duration: 3000 });
      }
    });
  }

  viewTenants(summary: ThemeAnalyticsSummary): void {
    this.dialog.open(ThemeUsageDialogComponent, {
      data: {
        themeId: summary.themeId,
        themeName: summary.themeName
      },
      width: '640px'
    });
  }
}
