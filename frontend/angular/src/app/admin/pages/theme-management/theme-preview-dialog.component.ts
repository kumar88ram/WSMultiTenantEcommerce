import { CommonModule } from '@angular/common';
import { Component, Inject, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';
import { ThemeService } from '../../../core/services/theme.service';
import { ThemePreviewResponse } from '../../../core/models/theme.models';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';

interface ThemePreviewDialogData {
  themeId: string;
  themeName: string;
  previewUrl: string;
  expiresAt: string;
}

@Component({
  selector: 'app-theme-preview-dialog',
  standalone: true,
  imports: [CommonModule, MatDialogModule, MatButtonModule, MatSnackBarModule],
  template: `
    <h2 mat-dialog-title>Preview: {{ data.themeName }}</h2>
    <mat-dialog-content>
      <div class="preview-container" *ngIf="safeUrl(); else loading">
        <iframe [src]="safeUrl()" title="Theme preview" class="preview-frame" (error)="refreshPreview()"></iframe>
      </div>
      <ng-template #loading>
        <p>Loading preview…</p>
      </ng-template>
      <p class="meta">Expires at {{ expiresAt() | date: 'medium' }}</p>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="refreshPreview()" [disabled]="refreshing()">Refresh</button>
      <button mat-button color="primary" mat-dialog-close>Close</button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      .preview-container {
        width: min(90vw, 960px);
        height: 70vh;
        border: 1px solid rgba(0, 0, 0, 0.12);
        border-radius: 8px;
        overflow: hidden;
      }
      .preview-frame {
        width: 100%;
        height: 100%;
        border: 0;
      }
      .meta {
        margin-top: 0.5rem;
        font-size: 0.85rem;
        color: rgba(0, 0, 0, 0.6);
      }
    `
  ]
})
export class ThemePreviewDialogComponent implements OnInit, OnDestroy {
  private readonly themeService = inject(ThemeService);
  private readonly sanitizer = inject(DomSanitizer);
  private readonly snackBar = inject(MatSnackBar);
  private refreshHandle: ReturnType<typeof setTimeout> | null = null;
  private readonly safeUrlSignal = signal<SafeResourceUrl | null>(null);
  private readonly expiresAtSignal = signal<Date>(new Date(this.data.expiresAt));
  protected readonly safeUrl = computed(() => this.safeUrlSignal());
  protected readonly expiresAt = computed(() => this.expiresAtSignal());
  protected readonly refreshing = signal(false);

  constructor(@Inject(MAT_DIALOG_DATA) public readonly data: ThemePreviewDialogData) {}

  ngOnInit(): void {
    this.safeUrlSignal.set(this.sanitizer.bypassSecurityTrustResourceUrl(this.data.previewUrl));
    this.scheduleRefresh();
  }

  ngOnDestroy(): void {
    if (this.refreshHandle) {
      clearTimeout(this.refreshHandle);
    }
  }

  refreshPreview(): void {
    if (this.refreshing()) {
      return;
    }

    if (this.refreshHandle) {
      clearTimeout(this.refreshHandle);
      this.refreshHandle = null;
    }

    this.refreshing.set(true);
    this.themeService.getThemePreviewUrl(this.data.themeId).subscribe({
      next: response => this.applyPreviewResponse(response),
      error: () => {
        this.snackBar.open('Failed to refresh preview token', 'Dismiss', { duration: 3000 });
        this.refreshing.set(false);
      }
    });
  }

  private applyPreviewResponse(response: ThemePreviewResponse): void {
    this.safeUrlSignal.set(this.sanitizer.bypassSecurityTrustResourceUrl(response.previewUrl));
    this.expiresAtSignal.set(new Date(response.expiresAt));
    this.refreshing.set(false);
    this.snackBar.open('Preview refreshed', 'Dismiss', { duration: 2000 });
    this.scheduleRefresh();
  }

  private scheduleRefresh(): void {
    if (this.refreshHandle) {
      clearTimeout(this.refreshHandle);
    }

    const expiresInMs = this.expiresAtSignal().getTime() - Date.now();
    if (expiresInMs <= 0) {
      this.refreshPreview();
      return;
    }

    const refreshDelay = Math.max(expiresInMs - 30_000, 5_000);
    this.refreshHandle = setTimeout(() => this.refreshPreview(), refreshDelay);
  }
}
