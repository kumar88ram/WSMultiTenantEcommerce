import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export type OrderStatus =
  | 'pending'
  | 'processing'
  | 'fulfilled'
  | 'shipped'
  | 'delivered'
  | 'cancelled'
  | 'refunded';

export interface OrderLineItem {
  id: string;
  name: string;
  quantity: number;
  unitPrice: number;
  total: number;
  currency: string;
  variantName?: string;
  imageUrl?: string;
}

export interface OrderSummary {
  id: string;
  number: string;
  status: OrderStatus;
  paymentStatus: 'pending' | 'authorized' | 'paid' | 'failed' | 'refunded';
  placedAt: string;
  total: number;
  currency: string;
}

export interface OrderDetail extends OrderSummary {
  items: OrderLineItem[];
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
}

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/store`;

  getOrderHistory(tenant: string, email: string): Observable<OrderSummary[]> {
    const params = new HttpParams().set('email', email);
    return this.http.get<OrderSummary[]>(`${this.baseUrl}/${tenant}/orders`, { params });
  }

  getOrderDetail(tenant: string, orderId: string): Observable<OrderDetail> {
    return this.http.get<OrderDetail>(`${this.baseUrl}/${tenant}/orders/${orderId}`);
  }
}
