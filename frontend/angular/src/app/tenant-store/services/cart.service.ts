import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface CartItem {
  id: string;
  productId: string;
  variantId?: string | null;
  name: string;
  quantity: number;
  unitPrice: number;
  currency: string;
  imageUrl?: string;
  lineTotal: number;
  metadata?: Record<string, unknown>;
}

export interface CartSummary {
  id: string;
  items: CartItem[];
  subtotal: number;
  taxTotal: number;
  shippingTotal: number;
  total: number;
  currency: string;
  updatedAt: string;
}

export interface AddCartItemRequest {
  productId: string;
  variantId?: string | null;
  quantity: number;
}

export interface UpdateCartItemQuantityRequest {
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/store`;

  private readonly cartSignal = signal<CartSummary | null>(null);
  private readonly loadingSignal = signal(false);
  private readonly errorSignal = signal<string | null>(null);

  readonly cart = this.cartSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();

  loadCart(tenant: string): Observable<CartSummary> {
    this.loadingSignal.set(true);
    this.errorSignal.set(null);

    return this.http.get<CartSummary>(`${this.baseUrl}/${tenant}/cart`).pipe(
      tap({
        next: cart => {
          this.cartSignal.set(cart);
          this.loadingSignal.set(false);
        },
        error: (error: unknown) => {
          this.loadingSignal.set(false);
          this.errorSignal.set('Unable to load the cart. Please try again later.');
          throw error;
        }
      })
    );
  }

  addItem(tenant: string, request: AddCartItemRequest): Observable<CartSummary> {
    this.errorSignal.set(null);
    this.loadingSignal.set(true);

    return this.http.post<CartSummary>(`${this.baseUrl}/${tenant}/cart/items`, request).pipe(
      tap({
        next: cart => {
          this.cartSignal.set(cart);
          this.loadingSignal.set(false);
        },
        error: (error: unknown) => {
          this.loadingSignal.set(false);
          this.errorSignal.set('Unable to add the item to the cart.');
          throw error;
        }
      })
    );
  }

  updateItemQuantity(tenant: string, itemId: string, quantity: number): Observable<CartSummary> {
    this.errorSignal.set(null);
    this.loadingSignal.set(true);

    const request: UpdateCartItemQuantityRequest = { quantity };
    return this.http
      .patch<CartSummary>(`${this.baseUrl}/${tenant}/cart/items/${itemId}`, request)
      .pipe(
        tap({
          next: cart => {
            this.cartSignal.set(cart);
            this.loadingSignal.set(false);
          },
          error: (error: unknown) => {
            this.loadingSignal.set(false);
            this.errorSignal.set('Unable to update the cart.');
            throw error;
          }
        })
      );
  }

  removeItem(tenant: string, itemId: string): Observable<CartSummary> {
    this.errorSignal.set(null);
    this.loadingSignal.set(true);

    return this.http.delete<CartSummary>(`${this.baseUrl}/${tenant}/cart/items/${itemId}`).pipe(
      tap({
        next: cart => {
          this.cartSignal.set(cart);
          this.loadingSignal.set(false);
        },
        error: (error: unknown) => {
          this.loadingSignal.set(false);
          this.errorSignal.set('Unable to remove the item from the cart.');
          throw error;
        }
      })
    );
  }

  clearCart(): void {
    this.cartSignal.set(null);
  }
}
