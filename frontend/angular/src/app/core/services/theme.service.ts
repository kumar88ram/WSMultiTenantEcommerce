import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';
import {
  TenantTheme,
  ThemeSectionsRequest,
  ThemeSummary,
  ThemeVariablesRequest
} from '../models/theme.models';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly http = inject(HttpClient);
  private readonly adminBaseUrl = `${environment.apiUrl}/admin/themes`;
  private readonly tenantBaseUrl = `${environment.apiUrl}/tenant/theme`;

  getThemes(): Observable<ThemeSummary[]> {
    return this.http.get<ThemeSummary[]>(this.adminBaseUrl);
  }

  uploadTheme(file: File): Observable<ThemeSummary> {
    const formData = new FormData();
    formData.append('package', file);
    return this.http.post<ThemeSummary>(`${this.adminBaseUrl}/upload`, formData);
  }

  activateTheme(themeId: string, tenantId: string): Observable<TenantTheme> {
    return this.http.post<TenantTheme>(`${this.adminBaseUrl}/${themeId}/activate/${tenantId}`, {});
  }

  deactivateTheme(themeId: string, tenantId: string): Observable<void> {
    return this.http.patch<void>(`${this.adminBaseUrl}/${themeId}/deactivate/${tenantId}`, {});
  }

  getActiveThemeForTenant(tenantId: string): Observable<TenantTheme> {
    return this.http.get<TenantTheme>(`${this.adminBaseUrl}/${tenantId}/active`);
  }

  saveSections(themeId: string, payload: ThemeSectionsRequest): Observable<void> {
    return this.http.post<void>(`${this.adminBaseUrl}/${themeId}/sections`, payload);
  }

  getTenantTheme(): Observable<TenantTheme> {
    return this.http.get<TenantTheme>(this.tenantBaseUrl);
  }

  updateTenantVariables(payload: ThemeVariablesRequest): Observable<void> {
    return this.http.patch<void>(`${this.tenantBaseUrl}/variables`, payload);
  }

  resetTenantVariables(): Observable<void> {
    return this.http.delete<void>(`${this.tenantBaseUrl}/variables`);
  }
}
