import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface TenantAdminSettings {
  seoTitle: string;
  seoDescription?: string;
  seoKeywords?: string;
  canonicalUrl?: string;
  indexable: boolean;
  logoUrl?: string;
}

export interface UploadLogoResponse {
  url: string;
}

@Injectable({ providedIn: 'root' })
export class TenantAdminSettingsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/tenant/settings`;

  getSettings(): Observable<TenantAdminSettings> {
    return this.http.get<TenantAdminSettings>(this.baseUrl);
  }

  saveSettings(settings: TenantAdminSettings): Observable<void> {
    return this.http.put<void>(this.baseUrl, settings);
  }

  uploadLogo(file: File): Observable<UploadLogoResponse> {
    const formData = new FormData();
    formData.append('file', file);
    return this.http.post<UploadLogoResponse>(`${this.baseUrl}/logo`, formData);
  }
}
