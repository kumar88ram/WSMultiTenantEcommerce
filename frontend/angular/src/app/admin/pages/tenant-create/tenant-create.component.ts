import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { finalize } from 'rxjs';
import { AdminTenantService } from '../../services/admin-tenant.service';

interface PlanOption {
  label: string;
  value: string;
  description: string;
}

@Component({
  selector: 'app-tenant-create',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <section class="tenant-create">
      <header>
        <h1>Create a new tenant</h1>
        <p>Provision a new tenant by defining domain settings and a subscription plan.</p>
      </header>

      <form [formGroup]="tenantForm" (ngSubmit)="onSubmit()" novalidate>
        <div class="field">
          <label for="name">Tenant name</label>
          <input id="name" type="text" formControlName="name" placeholder="Acme Inc." />
          <small class="error" *ngIf="shouldShowError('name')">A tenant name is required.</small>
        </div>

        <div class="field">
          <label for="subdomain">Subdomain</label>
          <div class="input-group">
            <input
              id="subdomain"
              type="text"
              formControlName="subdomain"
              placeholder="acme"
              autocomplete="off"
            />
            <span class="suffix">.example.com</span>
          </div>
          <small class="hint">Lowercase letters, numbers and hyphens only.</small>
          <small class="error" *ngIf="shouldShowError('subdomain')">
            Provide a subdomain using letters, numbers or hyphens.
          </small>
        </div>

        <div class="field">
          <label for="customDomain">Custom domain (optional)</label>
          <input
            id="customDomain"
            type="text"
            formControlName="customDomain"
            placeholder="store.acme.com"
            autocomplete="off"
          />
          <small class="error" *ngIf="shouldShowError('customDomain')">
            Enter a valid domain such as store.example.com.
          </small>
        </div>

        <fieldset class="plan-selector">
          <legend>Plan</legend>
          <div class="plan-options">
            <label *ngFor="let plan of planOptions" [class.selected]="tenantForm.get('plan')?.value === plan.value">
              <input type="radio" formControlName="plan" [value]="plan.value" />
              <span class="plan-label">{{ plan.label }}</span>
              <span class="plan-description">{{ plan.description }}</span>
            </label>
          </div>
          <small class="error" *ngIf="shouldShowError('plan')">Select a plan to continue.</small>
        </fieldset>

        <div class="actions">
          <button type="submit" [disabled]="tenantForm.invalid || isSubmitting()">Create tenant</button>
        </div>
      </form>

      <section class="progress" *ngIf="isProvisioning()">
        <p class="message">{{ provisioningMessage() }}</p>
        <div class="loader" aria-hidden="true"></div>
      </section>

      <section class="result success" *ngIf="createdTenantUrl()">
        <h2>Tenant ready</h2>
        <p>Your tenant has been provisioned successfully.</p>
        <a [href]="createdTenantUrl()" target="_blank" rel="noopener">Open tenant portal</a>
      </section>

      <section class="result error" *ngIf="submissionError()">
        <h2>Something went wrong</h2>
        <p>{{ submissionError() }}</p>
      </section>
    </section>
  `,
  styles: [
    `
      :host {
        display: block;
        padding: 2rem;
        max-width: 720px;
        margin: 0 auto;
      }

      header {
        margin-bottom: 2rem;
      }

      form {
        display: grid;
        gap: 1.5rem;
        background: #ffffff;
        padding: 2rem;
        border-radius: 1rem;
        box-shadow: 0 10px 35px rgba(0, 0, 0, 0.08);
      }

      .field {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      label,
      legend {
        font-weight: 600;
        color: #1f2933;
      }

      input[type='text'] {
        padding: 0.75rem 1rem;
        border-radius: 0.75rem;
        border: 1px solid #cbd2d9;
        font-size: 1rem;
        transition: border-color 0.2s ease, box-shadow 0.2s ease;
      }

      input[type='text']:focus {
        outline: none;
        border-color: #2563eb;
        box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.2);
      }

      .input-group {
        display: flex;
        align-items: center;
      }

      .input-group input {
        flex: 1;
        border-top-right-radius: 0;
        border-bottom-right-radius: 0;
      }

      .suffix {
        background: #f0f4f8;
        color: #52606d;
        border: 1px solid #cbd2d9;
        border-left: none;
        padding: 0.75rem 1rem;
        border-top-right-radius: 0.75rem;
        border-bottom-right-radius: 0.75rem;
      }

      .hint {
        color: #8292a2;
        font-size: 0.85rem;
      }

      fieldset.plan-selector {
        border: 1px solid #e4e7eb;
        border-radius: 1rem;
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .plan-options {
        display: grid;
        gap: 1rem;
      }

      .plan-options label {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        padding: 1rem;
        border: 1px solid #cbd2d9;
        border-radius: 0.75rem;
        cursor: pointer;
        transition: border-color 0.2s ease, box-shadow 0.2s ease;
      }

      .plan-options label.selected {
        border-color: #2563eb;
        box-shadow: 0 0 0 3px rgba(37, 99, 235, 0.2);
      }

      .plan-options input[type='radio'] {
        accent-color: #2563eb;
      }

      .plan-label {
        font-weight: 600;
        color: #1f2933;
      }

      .plan-description {
        color: #52606d;
        font-size: 0.95rem;
      }

      .actions {
        display: flex;
        justify-content: flex-end;
      }

      button[type='submit'] {
        background: #2563eb;
        color: #fff;
        padding: 0.9rem 2rem;
        border: none;
        border-radius: 0.75rem;
        font-weight: 600;
        cursor: pointer;
        transition: background 0.2s ease;
      }

      button[disabled] {
        opacity: 0.6;
        cursor: not-allowed;
      }

      .error {
        color: #d64545;
        font-size: 0.85rem;
      }

      .progress {
        margin-top: 2rem;
        display: flex;
        align-items: center;
        gap: 1rem;
        background: #f0f4ff;
        color: #1e3a8a;
        padding: 1rem 1.5rem;
        border-radius: 0.75rem;
      }

      .loader {
        width: 1.5rem;
        height: 1.5rem;
        border-radius: 50%;
        border: 3px solid rgba(37, 99, 235, 0.25);
        border-top-color: #2563eb;
        animation: spin 0.9s linear infinite;
      }

      @keyframes spin {
        to {
          transform: rotate(360deg);
        }
      }

      .result {
        margin-top: 2rem;
        padding: 1.5rem;
        border-radius: 0.75rem;
      }

      .result.success {
        background: #ecfdf3;
        color: #065f46;
        border: 1px solid #bbf7d0;
      }

      .result.error {
        background: #fef2f2;
        color: #991b1b;
        border: 1px solid #fecaca;
      }

      .result a {
        display: inline-block;
        margin-top: 0.75rem;
        color: #2563eb;
        text-decoration: none;
        font-weight: 600;
      }
    `
  ]
})
export class TenantCreateComponent {
  private readonly fb = inject(FormBuilder);
  private readonly adminTenantService = inject(AdminTenantService);

  readonly tenantForm = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.maxLength(120)]],
    subdomain: [
      '',
      [Validators.required, Validators.pattern(/^[a-z0-9]([a-z0-9-]*[a-z0-9])?$/)]
    ],
    customDomain: [
      '',
      [
        Validators.pattern(
          /^(|(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,})$/
        )
      ]
    ],
    plan: ['', Validators.required]
  });

  readonly planOptions: PlanOption[] = [
    { label: 'Starter', value: 'starter', description: 'Core features with standard support.' },
    { label: 'Growth', value: 'growth', description: 'Advanced integrations and priority support.' },
    { label: 'Scale', value: 'scale', description: 'Unlimited stores, dedicated success manager.' }
  ];

  private readonly isSubmittingSignal = signal(false);
  private readonly isProvisioningSignal = signal(false);
  private readonly provisioningMessageSignal = signal('');
  private readonly createdTenantUrlSignal = signal('');
  private readonly submissionErrorSignal = signal('');

  readonly isSubmitting = computed(() => this.isSubmittingSignal());
  readonly isProvisioning = computed(() => this.isProvisioningSignal());
  readonly provisioningMessage = computed(() => this.provisioningMessageSignal());
  readonly createdTenantUrl = computed(() => this.createdTenantUrlSignal());
  readonly submissionError = computed(() => this.submissionErrorSignal());

  shouldShowError(controlName: string): boolean {
    const control = this.tenantForm.get(controlName);
    return !!control && control.invalid && (control.dirty || control.touched);
  }

  onSubmit(): void {
    if (this.tenantForm.invalid) {
      this.tenantForm.markAllAsTouched();
      return;
    }

    this.submissionErrorSignal.set('');
    this.createdTenantUrlSignal.set('');
    this.isSubmittingSignal.set(true);
    this.isProvisioningSignal.set(true);
    this.provisioningMessageSignal.set('Creating tenant and provisioning database...');

    const { name, subdomain, customDomain, plan } = this.tenantForm.value;
    const sanitizedCustomDomain = customDomain?.trim() ?? '';

    this.adminTenantService
      .createTenant({
        name: name ?? '',
        subdomain: subdomain ?? '',
        customDomain: sanitizedCustomDomain ? sanitizedCustomDomain : undefined,
        plan: plan ?? ''
      })
      .pipe(finalize(() => this.isSubmittingSignal.set(false)))
      .subscribe({
        next: response => {
          this.provisioningMessageSignal.set('Finalizing configuration...');
          this.createdTenantUrlSignal.set(response.tenantUrl);
          this.isProvisioningSignal.set(false);
          this.tenantForm.reset({
            name: '',
            subdomain: '',
            customDomain: '',
            plan: ''
          });
        },
        error: error => {
          console.error('Failed to create tenant', error);
          this.isProvisioningSignal.set(false);
          this.submissionErrorSignal.set(
            error?.error?.message ?? 'We could not provision the tenant. Please try again.'
          );
        }
      });
  }
}
