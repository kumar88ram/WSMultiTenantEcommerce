import { CommonModule } from '@angular/common';
import { Component, Inject, OnInit, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ThemeService } from '../../../core/services/theme.service';
import { TenantThemeUsage } from '../../../core/models/theme.models';

interface ThemeUsageDialogData {
  themeId: string;
  themeName: string;
}

@Component({
  selector: 'app-theme-usage-dialog',
  standalone: true,
  imports: [
    CommonModule,
    MatDialogModule,
    MatButtonModule,
    MatListModule,
    MatProgressBarModule,
    MatSnackBarModule
  ],
  template: `
    <h2 mat-dialog-title>Tenants using {{ data.themeName }}</h2>
    <mat-dialog-content>
      <mat-progress-bar *ngIf="loading()" mode="indeterminate"></mat-progress-bar>
      <mat-list *ngIf="!loading()">
        <mat-list-item *ngFor="let usage of usages()">
          <div matListItemTitle>Tenant: {{ usage.tenantId }}</div>
          <div matListItemLine>
            Active Days: {{ usage.totalActiveDays | number: '1.0-1' }} · Activated {{ usage.activatedAt | date: 'medium' }}
          </div>
          <div matListItemLine *ngIf="usage.deactivatedAt">
            Deactivated {{ usage.deactivatedAt | date: 'medium' }}
          </div>
        </mat-list-item>
        <mat-list-item *ngIf="!usages().length">
          <div matListItemTitle>No tenant usage data available.</div>
        </mat-list-item>
      </mat-list>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close>Close</button>
    </mat-dialog-actions>
  `
})
export class ThemeUsageDialogComponent implements OnInit {
  private readonly themeService = inject(ThemeService);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly usages = signal<TenantThemeUsage[]>([]);
  protected readonly loading = signal(false);

  constructor(@Inject(MAT_DIALOG_DATA) public readonly data: ThemeUsageDialogData) {}

  ngOnInit(): void {
    this.loading.set(true);
    this.themeService.getThemeUsage(this.data.themeId).subscribe({
      next: response => {
        this.usages.set(response);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.snackBar.open('Failed to load theme usage', 'Dismiss', { duration: 3000 });
      }
    });
  }
}
