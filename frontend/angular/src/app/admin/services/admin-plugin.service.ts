import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminPlugin {
  id: string;
  name: string;
  description: string;
  version: string;
  category: string;
  enabled: boolean;
  tags?: string[];
}

@Injectable({
  providedIn: 'root'
})
export class AdminPluginService {
  private readonly baseUrl = `${environment.apiUrl}/admin/plugins`;

  constructor(private readonly http: HttpClient) {}

  getPlugins(): Observable<AdminPlugin[]> {
    return this.http.get<AdminPlugin[]>(this.baseUrl);
  }

  updatePluginState(pluginId: string, enabled: boolean): Observable<void> {
    return this.http.patch<void>(`${this.baseUrl}/${pluginId}`, { enabled });
  }
}
