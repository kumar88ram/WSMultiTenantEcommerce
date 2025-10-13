import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface MenuItem {
  id: string;
  label: string;
  url: string;
  children?: MenuItem[];
}

interface MenuResponse {
  items: MenuItem[];
}

@Injectable({ providedIn: 'root' })
export class TenantAdminMenuService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/tenant/menus`;

  getMenu(): Observable<MenuItem[]> {
    return this.http.get<MenuResponse>(this.baseUrl).pipe(map(response => response.items ?? []));
  }

  saveMenu(items: MenuItem[]): Observable<void> {
    return this.http.put<void>(this.baseUrl, { items });
  }
}
