import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ShippingMethod {
  id: string;
  name: string;
  description?: string;
  amount: number;
  currency: string;
  estimatedDaysMin?: number;
  estimatedDaysMax?: number;
}

export interface PaymentMethodOption {
  id: string;
  name: string;
  provider: string;
  type: 'card' | 'wallet' | 'bank_transfer' | 'cash_on_delivery' | string;
  instructions?: string;
}

export interface CheckoutConfiguration {
  shippingMethods: ShippingMethod[];
  paymentMethods: PaymentMethodOption[];
  requiresRedirect: boolean;
}

export interface ShippingAddressPayload {
  fullName: string;
  addressLine1: string;
  addressLine2?: string;
  city: string;
  region?: string;
  postalCode: string;
  country: string;
  phone?: string;
  email: string;
}

export interface CreateCheckoutSessionRequest {
  shippingAddress: ShippingAddressPayload;
  shippingMethodId: string;
  paymentMethodId: string;
  returnUrl: string;
  cancelUrl: string;
}

export interface CreateCheckoutSessionResponse {
  orderId: string;
  status: 'pending' | 'processing' | 'requires_payment_method' | 'requires_action' | 'paid' | 'failed';
  redirectUrl?: string;
  clientSecret?: string;
}

export interface PaymentStatusResponse {
  orderId: string;
  status: 'pending' | 'processing' | 'paid' | 'failed' | 'cancelled';
  paymentStatus: 'pending' | 'requires_action' | 'succeeded' | 'failed' | 'refunded';
  updatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/store`;

  getConfiguration(tenant: string): Observable<CheckoutConfiguration> {
    return this.http.get<CheckoutConfiguration>(`${this.baseUrl}/${tenant}/checkout/options`);
  }

  createCheckoutSession(
    tenant: string,
    request: CreateCheckoutSessionRequest
  ): Observable<CreateCheckoutSessionResponse> {
    return this.http.post<CreateCheckoutSessionResponse>(
      `${this.baseUrl}/${tenant}/checkout/session`,
      request
    );
  }

  getPaymentStatus(tenant: string, orderId: string): Observable<PaymentStatusResponse> {
    return this.http.get<PaymentStatusResponse>(
      `${this.baseUrl}/${tenant}/checkout/status/${orderId}`
    );
  }
}
