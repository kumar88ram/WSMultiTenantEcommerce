import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type AdminOrderStatus =
  | 'pending'
  | 'processing'
  | 'fulfilled'
  | 'shipped'
  | 'delivered'
  | 'cancelled'
  | 'refunded';

export interface AdminOrderSummary {
  id: string;
  number: string;
  tenantName: string;
  customerEmail: string;
  status: AdminOrderStatus;
  paymentStatus: 'pending' | 'authorized' | 'paid' | 'failed' | 'refunded';
  total: number;
  currency: string;
  placedAt: string;
}

export interface AdminOrderListResponse {
  items: AdminOrderSummary[];
  total: number;
}

export interface AdminOrderDetail extends AdminOrderSummary {
  items: Array<{
    id: string;
    name: string;
    quantity: number;
    unitPrice: number;
    total: number;
  }>;
  shippingAddress?: {
    fullName: string;
    addressLine1: string;
    addressLine2?: string;
    city: string;
    region?: string;
    postalCode: string;
    country: string;
  };
  shippingMethod?: string;
  paymentMethod?: string;
  subtotal: number;
  taxTotal: number;
  shippingTotal: number;
  metadata?: Record<string, unknown>;
}

@Injectable({ providedIn: 'root' })
export class AdminOrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/admin/orders`;

  getOrders(params?: { status?: string; page?: number; pageSize?: number }): Observable<AdminOrderListResponse> {
    let httpParams = new HttpParams();
    if (params?.status) {
      httpParams = httpParams.set('status', params.status);
    }

    if (params?.page != null) {
      httpParams = httpParams.set('page', params.page.toString());
    }

    if (params?.pageSize != null) {
      httpParams = httpParams.set('pageSize', params.pageSize.toString());
    }

    return this.http.get<AdminOrderListResponse>(this.baseUrl, { params: httpParams });
  }

  getOrder(orderId: string): Observable<AdminOrderDetail> {
    return this.http.get<AdminOrderDetail>(`${this.baseUrl}/${orderId}`);
  }

  updateStatus(orderId: string, status: AdminOrderStatus): Observable<AdminOrderDetail> {
    return this.http.patch<AdminOrderDetail>(`${this.baseUrl}/${orderId}/status`, { status });
  }
}
