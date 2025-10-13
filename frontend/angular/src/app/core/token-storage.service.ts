import { Injectable } from '@angular/core';

export interface StoredTokens {
  accessToken: string;
  refreshToken: string;
  expiresAt: Date;
  tenantHeader?: string;
}

@Injectable({ providedIn: 'root' })
export class TokenStorageService {
  private readonly accessTokenKey = 'mt_access';
  private readonly refreshTokenKey = 'mt_refresh';
  private readonly expiresKey = 'mt_expires';
  private readonly tenantKey = 'mt_tenant';

  setTokens(tokens: StoredTokens): void {
    localStorage.setItem(this.accessTokenKey, tokens.accessToken);
    localStorage.setItem(this.refreshTokenKey, tokens.refreshToken);
    localStorage.setItem(this.expiresKey, tokens.expiresAt.toISOString());
    if (tokens.tenantHeader) {
      localStorage.setItem(this.tenantKey, tokens.tenantHeader);
    } else {
      localStorage.removeItem(this.tenantKey);
    }
  }

  getTokens(): StoredTokens | null {
    const accessToken = localStorage.getItem(this.accessTokenKey);
    const refreshToken = localStorage.getItem(this.refreshTokenKey);
    const expiresAtRaw = localStorage.getItem(this.expiresKey);
    const tenant = localStorage.getItem(this.tenantKey) ?? undefined;

    if (!accessToken || !refreshToken || !expiresAtRaw) {
      return null;
    }

    return {
      accessToken,
      refreshToken,
      expiresAt: new Date(expiresAtRaw),
      tenantHeader: tenant
    };
  }

  clear(): void {
    localStorage.removeItem(this.accessTokenKey);
    localStorage.removeItem(this.refreshTokenKey);
    localStorage.removeItem(this.expiresKey);
    localStorage.removeItem(this.tenantKey);
  }
}
