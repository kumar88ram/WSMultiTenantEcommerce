import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { TokenStorageService } from '../token-storage.service';

export function provideAuthGuard(): CanActivateFn {
  return () => {
    const tokens = inject(TokenStorageService).getTokens();
    const router = inject(Router);

    if (!tokens || tokens.expiresAt < new Date()) {
      router.navigate(['/login']);
      return false;
    }

    return true;
  };
}
