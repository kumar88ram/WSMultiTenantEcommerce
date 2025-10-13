import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { firstValueFrom } from 'rxjs';
import { environment } from '../../../environments/environment';
import { TokenStorageService } from '../../core/token-storage.service';
import { TenantStoreService } from '../../tenant-store/services/tenant-store.service';

export interface LoginPayload {
  tenant: string;
  username: string;
  password: string;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenStorage = inject(TokenStorageService);
  private readonly tenantStore = inject(TenantStoreService);

  async login(payload: LoginPayload): Promise<void> {
    const response = await firstValueFrom(
      this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, {
        userName: payload.username,
        password: payload.password
      }, {
        headers: { 'X-Tenant': payload.tenant }
      })
    );

    this.tokenStorage.setTokens({
      accessToken: response.accessToken,
      refreshToken: response.refreshToken,
      expiresAt: new Date(response.expiresAt),
      tenantHeader: payload.tenant
    });

    this.tenantStore.setTenant(payload.tenant);

    await this.router.navigate(['/admin']);
  }

  async refresh(): Promise<void> {
    const tokens = this.tokenStorage.getTokens();
    if (!tokens) {
      return;
    }

    try {
      const response = await firstValueFrom(
        this.http.post<AuthResponse>(`${environment.apiUrl}/api/auth/refresh`, {
          refreshToken: tokens.refreshToken
        }, {
          headers: tokens.tenantHeader ? { 'X-Tenant': tokens.tenantHeader } : undefined
        })
      );

      this.tokenStorage.setTokens({
        accessToken: response.accessToken,
        refreshToken: response.refreshToken,
        expiresAt: new Date(response.expiresAt),
        tenantHeader: tokens.tenantHeader
      });
    } catch {
      this.logout();
    }
  }

  logout(): void {
    this.tokenStorage.clear();
    this.tenantStore.clear();
    this.router.navigate(['/login']);
  }
}
