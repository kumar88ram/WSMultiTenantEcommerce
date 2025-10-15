import { CommonModule } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AdminTenantService, TenantSummary } from '../../services/admin-tenant.service';
import { ThemeService } from '../../../core/services/theme.service';
import { ThemeCloneRequest } from '../../../core/models/theme.models';

@Component({
  selector: 'app-theme-clone-dialog',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatSelectModule,
    MatSnackBarModule,
    MatProgressBarModule
  ],
  template: `
    <h2 mat-dialog-title>Clone Theme</h2>
    <mat-dialog-content>
      <p>Select a source tenant to copy the active theme from and a destination tenant to receive the cloned configuration.</p>
      <form [formGroup]="form" class="clone-form">
        <mat-form-field appearance="outline">
          <mat-label>Source Tenant</mat-label>
          <mat-select formControlName="sourceTenantId" required>
            <mat-option *ngFor="let tenant of tenants()" [value]="tenant.id">
              {{ tenant.name }} ({{ tenant.subdomain }})
            </mat-option>
          </mat-select>
        </mat-form-field>
        <mat-form-field appearance="outline">
          <mat-label>Target Tenant</mat-label>
          <mat-select formControlName="targetTenantId" required>
            <mat-option *ngFor="let tenant of tenants()" [value]="tenant.id">
              {{ tenant.name }} ({{ tenant.subdomain }})
            </mat-option>
          </mat-select>
        </mat-form-field>
      </form>
      <mat-progress-bar *ngIf="saving()" mode="indeterminate"></mat-progress-bar>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button mat-dialog-close [disabled]="saving()">Cancel</button>
      <button mat-flat-button color="primary" (click)="submit()" [disabled]="form.invalid || saving()">
        Clone Theme
      </button>
    </mat-dialog-actions>
  `,
  styles: [
    `
      .clone-form {
        display: grid;
        gap: 1rem;
        margin-top: 1rem;
        min-width: min(80vw, 420px);
      }
    `
  ]
})
export class ThemeCloneDialogComponent implements OnInit {
  private readonly adminTenantService = inject(AdminTenantService);
  private readonly themeService = inject(ThemeService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly dialogRef = inject(MatDialogRef<ThemeCloneDialogComponent>);
  private readonly fb = inject(FormBuilder);

  protected readonly tenants = signal<TenantSummary[]>([]);
  protected readonly saving = signal(false);

  readonly form = this.fb.group({
    sourceTenantId: ['', Validators.required],
    targetTenantId: ['', Validators.required]
  });

  ngOnInit(): void {
    this.adminTenantService.getTenants().subscribe({
      next: tenants => this.tenants.set(tenants),
      error: () => this.snackBar.open('Unable to load tenants', 'Dismiss', { duration: 3000 })
    });
  }

  submit(): void {
    if (this.form.invalid || this.saving()) {
      return;
    }

    const payload = this.form.value as ThemeCloneRequest;
    if (payload.sourceTenantId === payload.targetTenantId) {
      this.snackBar.open('Source and target tenants must be different', 'Dismiss', { duration: 3000 });
      return;
    }

    this.saving.set(true);
    this.themeService.cloneTheme(payload).subscribe({
      next: response => {
        this.snackBar.open('Theme cloned successfully', 'Dismiss', { duration: 3000 });
        this.saving.set(false);
        this.dialogRef.close(response);
      },
      error: () => {
        this.snackBar.open('Failed to clone theme', 'Dismiss', { duration: 3000 });
        this.saving.set(false);
      }
    });
  }
}
