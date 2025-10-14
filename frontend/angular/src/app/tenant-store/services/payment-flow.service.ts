import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { Observable, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  CheckoutService,
  CreateCheckoutSessionRequest,
  CreateCheckoutSessionResponse,
  PaymentMethodOption,
  PaymentStatusResponse
} from './checkout.service';

export interface CardPaymentDetails {
  cardholderName?: string | null;
  cardNumber: string;
  expiryMonth: string;
  expiryYear: string;
  cvc: string;
}

export interface CardTokenizationResponse {
  token: string;
  createdAt: string;
  brand: string;
  last4: string;
}

@Injectable({ providedIn: 'root' })
export class PaymentFlowService {
  private readonly http = inject(HttpClient);
  private readonly checkoutService = inject(CheckoutService);

  initiatePayment(
    tenant: string,
    request: CreateCheckoutSessionRequest,
    paymentMethod: PaymentMethodOption,
    cardDetails?: CardPaymentDetails
  ): Observable<CreateCheckoutSessionResponse> {
    if (paymentMethod.flow === 'inline_card') {
      if (!cardDetails) {
        return throwError(() => new Error('Card details are required for inline card payments.'));
      }

      return this.tokenizeCard(cardDetails).pipe(
        switchMap(token => {
          const paymentData = {
            ...(request.paymentData ?? {}),
            token: token.token,
            brand: token.brand,
            last4: token.last4
          };

          return this.checkoutService.createCheckoutSession(tenant, {
            ...request,
            paymentData
          });
        })
      );
    }

    return this.checkoutService.createCheckoutSession(tenant, request);
  }

  handleReturn(tenant: string, orderId: string): Observable<PaymentStatusResponse> {
    return this.checkoutService.getPaymentStatus(tenant, orderId);
  }

  tokenizeCard(details: CardPaymentDetails): Observable<CardTokenizationResponse> {
    const payload = {
      cardNumber: details.cardNumber,
      expiryMonth: details.expiryMonth,
      expiryYear: details.expiryYear,
      cvc: details.cvc,
      cardholderName: details.cardholderName ?? null
    };

    return this.http.post<CardTokenizationResponse>(
      `${environment.apiUrl}/api/payments/stripe/tokenize`,
      payload
    );
  }
}
