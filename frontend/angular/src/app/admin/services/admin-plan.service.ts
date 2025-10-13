import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface AdminPlan {
  id: string;
  name: string;
  price: number;
  interval: 'monthly' | 'yearly';
  features: string[];
  tags?: string[];
  recommended?: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class AdminPlanService {
  private readonly baseUrl = `${environment.apiUrl}/admin/plans`;

  constructor(private readonly http: HttpClient) {}

  getPlans(): Observable<AdminPlan[]> {
    return this.http.get<AdminPlan[]>(this.baseUrl);
  }
}
