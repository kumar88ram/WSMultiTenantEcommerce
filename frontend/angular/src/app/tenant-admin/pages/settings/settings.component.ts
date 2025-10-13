import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { Subscription } from 'rxjs';
import {
  TenantAdminSettings,
  TenantAdminSettingsService
} from '../../services/tenant-admin-settings.service';

@Component({
  selector: 'app-tenant-settings',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressBarModule,
    MatSlideToggleModule
  ],
  template: `
    <section class="settings" *ngIf="form">
      <header>
        <div>
          <h1>Storefront settings</h1>
          <p class="subtitle">Control SEO metadata, brand assets and tracking preferences.</p>
        </div>
        <button mat-flat-button color="primary" (click)="save()" [disabled]="form.invalid || saving()">
          <mat-icon>save</mat-icon>
          Save settings
        </button>
      </header>

      <div class="grid">
        <mat-card>
          <mat-card-header>
            <mat-card-title>SEO metadata</mat-card-title>
            <mat-card-subtitle>Improve discoverability with accurate meta tags.</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <form [formGroup]="form" class="form-grid">
              <mat-form-field appearance="outline">
                <mat-label>Meta title</mat-label>
                <input matInput formControlName="seoTitle" placeholder="Storefront title" required />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Meta description</mat-label>
                <textarea matInput rows="4" formControlName="seoDescription"></textarea>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Keywords</mat-label>
                <input matInput formControlName="seoKeywords" placeholder="commerce, fashion" />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Canonical URL</mat-label>
                <input matInput formControlName="canonicalUrl" placeholder="https://store.example.com" />
              </mat-form-field>

              <mat-slide-toggle formControlName="indexable">Allow search engines to index</mat-slide-toggle>
            </form>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header>
            <mat-card-title>Brand assets</mat-card-title>
            <mat-card-subtitle>Upload an updated logo used across storefront surfaces.</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="logo-preview" [class.uploading]="uploading()">
              <img *ngIf="logoUrl()" [src]="logoUrl()" alt="Logo" />
              <button mat-stroked-button type="button" color="primary" (click)="fileInput.click()">
                <mat-icon>upload</mat-icon>
                {{ logoUrl() ? 'Replace logo' : 'Upload logo' }}
              </button>
              <input #fileInput type="file" accept="image/*" hidden (change)="onLogoSelected($event)" />
            </div>
            <mat-progress-bar *ngIf="uploading()" mode="indeterminate"></mat-progress-bar>
          </mat-card-content>
        </mat-card>
      </div>
    </section>
  `,
  styles: [
    `
      .settings {
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
      }

      header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        flex-wrap: wrap;
        gap: 1rem;
      }

      .subtitle {
        color: rgba(0, 0, 0, 0.6);
        margin: 0;
      }

      .grid {
        display: grid;
        grid-template-columns: 2fr 1fr;
        gap: 1.5rem;
        align-items: start;
      }

      .form-grid {
        display: grid;
        gap: 1rem;
      }

      .logo-preview {
        display: grid;
        gap: 1rem;
        justify-items: center;
      }

      .logo-preview img {
        max-width: 200px;
        border-radius: 0.75rem;
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
      }

      .logo-preview.uploading {
        opacity: 0.6;
        pointer-events: none;
      }

      @media (max-width: 1024px) {
        .grid {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class SettingsComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly settingsService = inject(TenantAdminSettingsService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly subscription = new Subscription();

  protected readonly logoUrl = signal<string | null>(null);
  protected readonly uploading = signal(false);
  protected readonly saving = signal(false);

  form = this.fb.nonNullable.group({
    seoTitle: ['', Validators.required],
    seoDescription: [''],
    seoKeywords: [''],
    canonicalUrl: [''],
    indexable: [true]
  });

  ngOnInit(): void {
    const loadSub = this.settingsService.getSettings().subscribe(settings => {
      this.form.patchValue({
        seoTitle: settings.seoTitle ?? '',
        seoDescription: settings.seoDescription ?? '',
        seoKeywords: settings.seoKeywords ?? '',
        canonicalUrl: settings.canonicalUrl ?? '',
        indexable: settings.indexable ?? true
      });
      this.logoUrl.set(settings.logoUrl ?? null);
    });

    this.subscription.add(loadSub);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  onLogoSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) {
      return;
    }

    this.uploading.set(true);
    const uploadSub = this.settingsService.uploadLogo(file).subscribe({
      next: response => {
        this.logoUrl.set(response.url);
        this.uploading.set(false);
        this.snackBar.open('Logo uploaded', 'Close', { duration: 2000 });
      },
      error: () => {
        this.uploading.set(false);
        this.snackBar.open('Failed to upload logo', 'Close', { duration: 3000 });
      }
    });

    this.subscription.add(uploadSub);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const payload: TenantAdminSettings = {
      ...this.form.getRawValue(),
      logoUrl: this.logoUrl() ?? undefined
    };

    const saveSub = this.settingsService.saveSettings(payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Settings updated', 'Close', { duration: 2000 });
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Failed to update settings', 'Close', { duration: 3000 });
      }
    });

    this.subscription.add(saveSub);
  }
}
