import { CommonModule } from '@angular/common';
import { Component, Input, inject, signal } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { ThemeSummary } from '../../../core/models/theme.models';
import { ThemeService } from '../../../core/services/theme.service';
import { ThemePreviewDialogComponent } from './theme-preview-dialog.component';

@Component({
  selector: 'app-theme-preview',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatChipsModule,
    MatButtonModule,
    MatIconModule,
    MatDialogModule,
    MatProgressSpinnerModule,
    MatSnackBarModule,
    ThemePreviewDialogComponent
  ],
  template: `
    <mat-card *ngIf="theme; else empty">
      <mat-card-header>
        <mat-card-title>{{ theme?.name }}</mat-card-title>
        <mat-card-subtitle>{{ theme?.code }} · v{{ theme?.version }}</mat-card-subtitle>
      </mat-card-header>
      <img mat-card-image *ngIf="theme?.previewImageUrl" [src]="theme?.previewImageUrl" alt="Theme preview" />
      <mat-card-content>
        <p>{{ theme?.description || 'No description provided.' }}</p>
        <div class="sections" *ngIf="theme?.sections?.length">
          <span>Sections:</span>
          <mat-chip-listbox>
            <mat-chip *ngFor="let section of theme!.sections">{{ section.sectionName }}</mat-chip>
          </mat-chip-listbox>
        </div>
      </mat-card-content>
      <mat-card-actions align="end">
        <button mat-stroked-button color="primary" (click)="openPreview()" [disabled]="loadingPreview()">
          <mat-icon>visibility</mat-icon>
          Preview
          <mat-progress-spinner
            *ngIf="loadingPreview()"
            diameter="18"
            mode="indeterminate"
            class="inline-spinner"
          ></mat-progress-spinner>
        </button>
        <button mat-stroked-button (click)="exportTheme()" [disabled]="exporting()">
          <mat-icon>download</mat-icon>
          Export
          <mat-progress-spinner
            *ngIf="exporting()"
            diameter="18"
            mode="indeterminate"
            class="inline-spinner"
          ></mat-progress-spinner>
        </button>
      </mat-card-actions>
    </mat-card>
    <ng-template #empty>
      <mat-card>
        <mat-card-content>
          <p>Select a theme to preview its details.</p>
        </mat-card-content>
      </mat-card>
    </ng-template>
  `,
  styles: [
    `
      mat-card {
        width: 100%;
      }
      .sections {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        margin-top: 1rem;
      }
      .inline-spinner {
        margin-left: 0.5rem;
      }
      mat-card-actions button {
        display: inline-flex;
        align-items: center;
        gap: 0.5rem;
      }
    `
  ]
})
export class ThemePreviewComponent {
  @Input() theme: ThemeSummary | null = null;

  private readonly themeService = inject(ThemeService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly loadingPreview = signal(false);
  protected readonly exporting = signal(false);

  openPreview(): void {
    if (!this.theme || this.loadingPreview()) {
      return;
    }

    this.loadingPreview.set(true);
    this.themeService.getThemePreviewUrl(this.theme.id).subscribe({
      next: response => {
        this.loadingPreview.set(false);
        this.dialog.open(ThemePreviewDialogComponent, {
          data: {
            themeId: this.theme!.id,
            themeName: this.theme!.name,
            previewUrl: response.previewUrl,
            expiresAt: response.expiresAt
          },
          autoFocus: false,
          width: '960px'
        });
      },
      error: () => {
        this.loadingPreview.set(false);
        this.snackBar.open('Unable to load theme preview', 'Dismiss', { duration: 3000 });
      }
    });
  }

  exportTheme(): void {
    if (!this.theme || this.exporting()) {
      return;
    }

    this.exporting.set(true);
    this.themeService.exportTheme(this.theme.id).subscribe({
      next: blob => {
        const url = window.URL.createObjectURL(blob);
        const anchor = document.createElement('a');
        anchor.href = url;
        anchor.download = `${this.theme!.code || 'theme'}-export.zip`;
        anchor.click();
        window.URL.revokeObjectURL(url);
        this.snackBar.open('Theme exported successfully', 'Dismiss', { duration: 3000 });
        this.exporting.set(false);
      },
      error: () => {
        this.snackBar.open('Failed to export theme', 'Dismiss', { duration: 3000 });
        this.exporting.set(false);
      }
    });
  }
}
