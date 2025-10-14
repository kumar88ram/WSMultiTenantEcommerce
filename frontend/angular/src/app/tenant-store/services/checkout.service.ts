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

export type PaymentFlow = 'hosted_redirect' | 'inline_card' | string;

export interface PaymentMethodOption {
  id: string;
  name: string;
  provider: string;
  flow: PaymentFlow;
  instructions?: string;
  metadata?: Record<string, string>;
}

export interface CheckoutConfiguration {
  shippingMethods: ShippingMethod[];
  paymentMethods: PaymentMethodOption[];
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
  cartId?: string;
  userId?: string;
  guestToken?: string;
  shippingAddress: ShippingAddressPayload;
  shippingMethodId: string;
  paymentMethodId: string;
  returnUrl: string;
  cancelUrl: string;
  couponCode?: string;
  paymentData?: Record<string, string>;
}

export interface CreateCheckoutSessionResponse {
  orderId: string;
  status: string;
  redirectUrl?: string;
  clientSecret?: string;
}

export interface PaymentStatusResponse {
  orderId: string;
  status: string;
  paymentStatus: string;
  updatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/store`;

  getConfiguration(tenant: string): Observable<CheckoutConfiguration> {
    return this.http.get<CheckoutConfiguration>(`${this.baseUrl}/${tenant}/checkout/options`, {
      headers: { 'X-Tenant': tenant }
    });
  }

  createCheckoutSession(
    tenant: string,
    request: CreateCheckoutSessionRequest
  ): Observable<CreateCheckoutSessionResponse> {
    return this.http.post<CreateCheckoutSessionResponse>(
      `${this.baseUrl}/${tenant}/checkout/session`,
      request,
      { headers: { 'X-Tenant': tenant } }
    );
  }

  getPaymentStatus(tenant: string, orderId: string): Observable<PaymentStatusResponse> {
    return this.http.get<PaymentStatusResponse>(
      `${this.baseUrl}/${tenant}/checkout/status/${orderId}`,
      { headers: { 'X-Tenant': tenant } }
    );
  }
}
