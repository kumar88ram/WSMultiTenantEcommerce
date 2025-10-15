import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { ThemeService } from '../../../core/services/theme.service';
import { ThemeSummary, TenantTheme } from '../../../core/models/theme.models';

@Component({
  selector: 'app-theme-activation',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule
  ],
  template: `
    <section class="activation" *ngIf="theme">
      <form [formGroup]="form" (ngSubmit)="activate()">
        <mat-form-field appearance="outline">
          <mat-label>Tenant ID</mat-label>
          <input matInput placeholder="00000000-0000-0000-0000-000000000000" formControlName="tenantId" />
        </mat-form-field>
        <div class="actions">
          <button mat-stroked-button color="primary" type="submit" [disabled]="form.invalid || working">
            Activate for Tenant
          </button>
          <button mat-stroked-button color="warn" type="button" (click)="deactivate()" [disabled]="form.invalid || working">
            Deactivate
          </button>
        </div>
      </form>
    </section>
  `,
  styles: [
    `
      .activation {
        display: flex;
        flex-direction: column;
        gap: 1rem;
        padding: 1rem 0;
      }
      .actions {
        display: flex;
        gap: 0.5rem;
      }
    `
  ]
})
export class ThemeActivationComponent {
  @Input() theme: ThemeSummary | null = null;
  @Output() readonly themeActivated = new EventEmitter<TenantTheme>();
  @Output() readonly themeDeactivated = new EventEmitter<void>();

  protected working = false;

  private readonly themeService = inject(ThemeService);
  private readonly fb = inject(FormBuilder);

  protected readonly form = this.fb.nonNullable.group({
    tenantId: ['', [Validators.required]]
  });

  activate(): void {
    if (!this.theme || this.form.invalid) {
      return;
    }

    this.working = true;
    const tenantId = this.form.value.tenantId!;
    this.themeService.activateTheme(this.theme.id, tenantId).subscribe({
      next: result => {
        this.working = false;
        this.themeActivated.emit(result);
      },
      error: () => {
        this.working = false;
      }
    });
  }

  deactivate(): void {
    if (!this.theme || this.form.invalid) {
      return;
    }

    this.working = true;
    const tenantId = this.form.value.tenantId!;
    this.themeService.deactivateTheme(this.theme.id, tenantId).subscribe({
      next: () => {
        this.working = false;
        this.themeDeactivated.emit();
      },
      error: () => {
        this.working = false;
      }
    });
  }
}
