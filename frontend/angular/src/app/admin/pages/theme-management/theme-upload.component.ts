import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Output, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { ThemeService } from '../../../core/services/theme.service';
import { ThemeSummary } from '../../../core/models/theme.models';

@Component({
  selector: 'app-theme-upload',
  standalone: true,
  imports: [CommonModule, MatButtonModule, MatIconModule, MatProgressBarModule],
  template: `
    <div class="theme-upload">
      <input type="file" accept=".zip" (change)="onFileSelected($event)" hidden #fileInput />
      <button mat-raised-button color="primary" (click)="fileInput.click()" [disabled]="uploading">
        <mat-icon>cloud_upload</mat-icon>
        <span>Upload Theme</span>
      </button>
      <mat-progress-bar *ngIf="uploading" mode="indeterminate"></mat-progress-bar>
    </div>
  `,
  styles: [
    `
      .theme-upload {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
      }
      button {
        align-self: flex-start;
      }
    `
  ]
})
export class ThemeUploadComponent {
  @Output() readonly themeUploaded = new EventEmitter<ThemeSummary>();

  protected uploading = false;

  private readonly themeService = inject(ThemeService);

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files || input.files.length === 0) {
      return;
    }

    const file = input.files[0];
    this.uploading = true;
    this.themeService.uploadTheme(file).subscribe({
      next: theme => {
        this.uploading = false;
        input.value = '';
        this.themeUploaded.emit(theme);
      },
      error: () => {
        this.uploading = false;
      }
    });
  }
}
