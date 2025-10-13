import { inject } from '@angular/core';
import { CanActivateFn, Router, UrlTree } from '@angular/router';
import { TokenStorageService } from '../../core/token-storage.service';

function evaluateTenantAccess(): boolean | UrlTree {
  const tokenService = inject(TokenStorageService);
  const router = inject(Router);
  const tokens = tokenService.getTokens();

  if (!tokens) {
    return router.parseUrl('/login');
  }

  if (!tokens.tenantHeader) {
    return router.parseUrl('/admin');
  }

  return true;
}

export function provideTenantAdminGuard(): CanActivateFn {
  return () => evaluateTenantAccess();
}
