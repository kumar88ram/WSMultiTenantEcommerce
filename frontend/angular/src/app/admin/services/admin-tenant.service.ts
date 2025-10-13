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

@Injectable({
  providedIn: 'root'
})
export class AdminTenantService {
  private readonly baseUrl = `${environment.apiUrl}/admin/tenants`;

  constructor(private readonly http: HttpClient) {}

  createTenant(payload: CreateTenantRequest): Observable<CreateTenantResponse> {
    return this.http.post<CreateTenantResponse>(this.baseUrl, payload);
  }
}
