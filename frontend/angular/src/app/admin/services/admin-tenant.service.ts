import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CreateTenantRequest {
  name: string;
  subdomain: string;
  customDomain?: string | null;
  plan: string;
}

export interface CreateTenantResponse {
  tenantId: string;
  tenantUrl: string;
}

export interface TenantSummary {
  id: string;
  name: string;
  subdomain: string;
  rootDomain: string;
  customDomain?: string | null;
  plan: string;
  status: 'active' | 'suspended' | 'pending';
  createdAt: string;
}

export interface TenantIntegration {
  id: string;
  name: string;
  description: string;
  status: 'active' | 'disabled' | 'error';
  icon: string;
}

export interface TenantDetail extends TenantSummary {
  ownerName: string;
  ownerEmail: string;
  renewalDate: string;
  mrr: number;
  metrics: {
    orders: number;
    revenue: number;
    activeUsers: number;
  };
  integrations: TenantIntegration[];
}

@Injectable({
  providedIn: 'root'
})
export class AdminTenantService {
  private readonly baseUrl = `${environment.apiUrl}/admin/tenants`;

  constructor(private readonly http: HttpClient) {}

  createTenant(payload: CreateTenantRequest): Observable<CreateTenantResponse> {
    return this.http.post<CreateTenantResponse>(this.baseUrl, payload);
  }

  getTenants(): Observable<TenantSummary[]> {
    return this.http.get<TenantSummary[]>(this.baseUrl);
  }

  getTenant(tenantId: string): Observable<TenantDetail> {
    return this.http.get<TenantDetail>(`${this.baseUrl}/${tenantId}`);
  }
}
