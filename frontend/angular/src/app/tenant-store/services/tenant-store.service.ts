import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class TenantStoreService {
  private readonly tenantSignal = signal<string | null>(null);

  readonly tenant$ = this.tenantSignal.asReadonly();

  setTenant(identifier: string): void {
    this.tenantSignal.set(identifier);
  }

  clear(): void {
    this.tenantSignal.set(null);
  }
}
