import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { ThemeService } from '../../../core/services/theme.service';
import { ThemeVariable, TenantTheme } from '../../../core/models/theme.models';
import { ThemePreviewComponent } from './theme-preview.component';

@Component({
  selector: 'app-theme-customizer',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSnackBarModule,
    ThemePreviewComponent
  ],
  template: `
    <section class="customizer" *ngIf="theme() as current">
      <div class="grid">
        <mat-card class="settings">
          <mat-card-header>
            <mat-card-title>Customize Theme</mat-card-title>
            <mat-card-subtitle>{{ current.theme.name }} · {{ current.theme.code }}</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <form [formGroup]="form" (ngSubmit)="save()">
              <div class="form-grid">
                <mat-form-field appearance="outline">
                  <mat-label>Primary Color</mat-label>
                  <input matInput type="color" formControlName="primaryColor" />
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Accent Color</mat-label>
                  <input matInput type="color" formControlName="accentColor" />
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Font Family</mat-label>
                  <input matInput formControlName="fontFamily" placeholder="e.g. Inter, sans-serif" />
                </mat-form-field>
                <mat-form-field appearance="outline">
                  <mat-label>Logo URL</mat-label>
                  <input matInput formControlName="logoUrl" />
                </mat-form-field>
              </div>
              <div class="actions">
                <button mat-flat-button color="primary" type="submit" [disabled]="saving()">
                  Save Changes
                </button>
                <button mat-stroked-button color="warn" type="button" (click)="reset()" [disabled]="saving()">
                  Revert to Default
                </button>
              </div>
            </form>
          </mat-card-content>
        </mat-card>

        <mat-card class="preview-card">
          <mat-card-header>
            <mat-card-title>Live Preview</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <app-tenant-theme-preview [previewUrl]="previewUrl()"></app-tenant-theme-preview>
            <div class="variables">
              <h4>Applied Variables</h4>
              <ul>
                <li *ngFor="let variable of variables()">{{ variable.key }}: {{ variable.value }}</li>
              </ul>
            </div>
          </mat-card-content>
        </mat-card>
      </div>
    </section>
    <section class="loading" *ngIf="loading()">
      <p>Loading theme customizer...</p>
    </section>
  `,
  styles: [
    `
      .customizer {
        display: block;
      }
      .grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
        gap: 1.5rem;
      }
      .form-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        gap: 1rem;
      }
      .actions {
        margin-top: 1.5rem;
        display: flex;
        gap: 1rem;
      }
      .preview-card {
        display: flex;
        flex-direction: column;
      }
      .variables {
        margin-top: 1.5rem;
      }
      .variables ul {
        padding-left: 1rem;
      }
      .loading {
        text-align: center;
        color: rgba(0, 0, 0, 0.6);
        padding: 2rem 0;
      }
    `
  ]
})
export class ThemeCustomizerComponent implements OnInit {
  private readonly themeService = inject(ThemeService);
  private readonly fb = inject(FormBuilder);
  private readonly snackBar = inject(MatSnackBar);

  protected readonly loading = signal(true);
  protected readonly saving = signal(false);
  private readonly themeSignal = signal<TenantTheme | null>(null);

  protected readonly theme = computed(() => this.themeSignal());
  protected readonly variables = computed<ThemeVariable[]>(() => this.themeSignal()?.variables ?? []);
  protected readonly previewUrl = computed(() => this.themeSignal()?.theme.previewImageUrl ?? null);

  protected readonly form = this.fb.nonNullable.group({
    primaryColor: ['#3f51b5'],
    accentColor: ['#ff4081'],
    fontFamily: ['Inter, sans-serif'],
    logoUrl: ['']
  });

  ngOnInit(): void {
    this.loadTheme();
  }

  save(): void {
    if (!this.themeSignal()) {
      return;
    }

    this.saving.set(true);
    const payload = {
      variables: {
        primaryColor: this.form.value.primaryColor ?? '#3f51b5',
        accentColor: this.form.value.accentColor ?? '#ff4081',
        fontFamily: this.form.value.fontFamily ?? 'Inter, sans-serif',
        logoUrl: this.form.value.logoUrl ?? ''
      }
    };

    this.themeService.updateTenantVariables(payload).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Theme variables saved', 'Close', { duration: 2500 });
        this.loadTheme();
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Failed to save theme variables', 'Close', { duration: 2500 });
      }
    });
  }

  reset(): void {
    this.saving.set(true);
    this.themeService.resetTenantVariables().subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Theme variables reset', 'Close', { duration: 2500 });
        this.loadTheme();
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Unable to reset variables', 'Close', { duration: 2500 });
      }
    });
  }

  private loadTheme(): void {
    this.loading.set(true);
    this.themeService.getTenantTheme().subscribe({
      next: theme => {
        this.themeSignal.set(theme);
        this.applyVariables(theme.variables);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  private applyVariables(variables: ThemeVariable[]): void {
    const lookup = new Map<string, string>(variables.map(v => [v.key, v.value]));
    this.form.patchValue({
      primaryColor: lookup.get('primaryColor') ?? '#3f51b5',
      accentColor: lookup.get('accentColor') ?? '#ff4081',
      fontFamily: lookup.get('fontFamily') ?? 'Inter, sans-serif',
      logoUrl: lookup.get('logoUrl') ?? ''
    });
  }
}
