import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../../auth/services/auth.service';
import { TokenStorageService } from '../token-storage.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const tokenStorage = inject(TokenStorageService);
  const authService = inject(AuthService);
  const tokens = tokenStorage.getTokens();

  if (tokens?.expiresAt && tokens.expiresAt < new Date()) {
    authService.refresh();
  }

  if (tokens?.accessToken) {
    req = req.clone({
      setHeaders: {
        Authorization: `Bearer ${tokens.accessToken}`,
        ...(tokens.tenantHeader ? { 'X-Tenant': tokens.tenantHeader } : {})
      }
    });
  }

  return next(req);
};
