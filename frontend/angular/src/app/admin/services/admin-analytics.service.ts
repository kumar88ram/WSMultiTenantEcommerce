import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AnalyticsKpi {
  label: string;
  value: string;
  deltaLabel: string;
}

export interface RevenuePoint {
  label: string;
  value: number;
}

export interface PlanSegment {
  plan: string;
  count: number;
  percentage: number;
}

export interface TopTenant {
  name: string;
  category: string;
  revenue: number;
}

export interface AdminAnalyticsOverview {
  generatedAt: string;
  kpis: AnalyticsKpi[];
  revenueTrend: RevenuePoint[];
  activeTenants: number;
  totalTenants: number;
  activeTenantsRatio: number;
  tenantsByPlan: PlanSegment[];
  topTenants: TopTenant[];
}

@Injectable({
  providedIn: 'root'
})
export class AdminAnalyticsService {
  private readonly baseUrl = `${environment.apiUrl}/admin/analytics`;

  constructor(private readonly http: HttpClient) {}

  getOverview(): Observable<AdminAnalyticsOverview> {
    return this.http.get<AdminAnalyticsOverview>(`${this.baseUrl}/overview`);
  }
}
