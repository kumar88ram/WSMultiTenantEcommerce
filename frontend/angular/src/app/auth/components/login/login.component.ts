import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="login-container">
      <h2>Tenant Login</h2>
      <form [formGroup]="form" (ngSubmit)="submit()">
        <label>
          Tenant Identifier
          <input type="text" formControlName="tenant" placeholder="tenant slug" />
        </label>
        <label>
          Username
          <input type="text" formControlName="username" />
        </label>
        <label>
          Password
          <input type="password" formControlName="password" />
        </label>
        <button type="submit" [disabled]="form.invalid || loading">Login</button>
      </form>
      <p *ngIf="error" class="error">{{ error }}</p>
    </div>
  `,
  styles: [`
    .login-container { max-width: 360px; margin: 3rem auto; display: flex; flex-direction: column; gap: 1rem; }
    form { display: flex; flex-direction: column; gap: 0.75rem; }
    label { display: flex; flex-direction: column; font-weight: 600; }
    input { padding: 0.5rem; }
    button { padding: 0.5rem; }
    .error { color: #b00020; }
  `]
})
export class LoginComponent {
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);

  form = this.fb.group({
    tenant: ['', Validators.required],
    username: ['', Validators.required],
    password: ['', Validators.required]
  });

  loading = false;
  error: string | null = null;

  async submit() {
    if (this.form.invalid) {
      return;
    }

    this.loading = true;
    this.error = null;
    const { tenant, username, password } = this.form.value;

    try {
      await this.authService.login({
        tenant: tenant!,
        username: username!,
        password: password!
      });
    } catch (err) {
      this.error = 'Login failed';
    } finally {
      this.loading = false;
    }
  }
}
