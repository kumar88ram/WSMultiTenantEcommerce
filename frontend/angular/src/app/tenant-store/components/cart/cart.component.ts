import { CommonModule } from '@angular/common';
import { Component, Signal, computed, effect, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { CartItem, CartService } from '../../services/cart.service';
import { TenantStoreService } from '../../services/tenant-store.service';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="cart" *ngIf="!loading(); else loadingState">
      <h1>Your cart</h1>
      <p class="cart-error" *ngIf="error()">{{ error() }}</p>

      <div class="cart-content" *ngIf="cart(); else emptyState">
        <section class="cart-items">
          <article class="cart-item" *ngFor="let item of cart()?.items; trackBy: trackByItemId">
            <div class="item-media" *ngIf="item.imageUrl">
              <img [src]="item.imageUrl" [alt]="item.name" />
            </div>

            <div class="item-details">
              <h2>{{ item.name }}</h2>
              <p class="variant" *ngIf="item.metadata?.variantName">{{ item.metadata?.variantName }}</p>
              <p class="price">{{ item.unitPrice | currency: item.currency }}</p>

              <div class="item-actions">
                <label class="quantity-label">
                  Qty
                  <input
                    type="number"
                    min="1"
                    [ngModel]="item.quantity"
                    (ngModelChange)="onQuantityChange(item, $event)"
                  />
                </label>

                <button type="button" class="remove" (click)="removeItem(item)">
                  Remove
                </button>
              </div>
            </div>

            <div class="line-total">
              {{ item.lineTotal | currency: item.currency }}
            </div>
          </article>
        </section>

        <aside class="summary" *ngIf="cart() as currentCart">
          <h2>Order summary</h2>
          <dl>
            <div>
              <dt>Items</dt>
              <dd>{{ totalItems() }}</dd>
            </div>
            <div>
              <dt>Subtotal</dt>
              <dd>{{ currentCart.subtotal | currency: currentCart.currency }}</dd>
            </div>
            <div>
              <dt>Shipping</dt>
              <dd>{{ currentCart.shippingTotal | currency: currentCart.currency }}</dd>
            </div>
            <div>
              <dt>Taxes</dt>
              <dd>{{ currentCart.taxTotal | currency: currentCart.currency }}</dd>
            </div>
            <div class="total">
              <dt>Total</dt>
              <dd>{{ currentCart.total | currency: currentCart.currency }}</dd>
            </div>
          </dl>

          <a class="checkout" [routerLink]="['/store', tenant(), 'checkout']">
            Proceed to checkout
          </a>
        </aside>
      </div>
    </div>

    <ng-template #loadingState>
      <div class="loading">Loading your cart…</div>
    </ng-template>

    <ng-template #emptyState>
      <div class="empty">
        <p>Your cart is empty.</p>
        <a [routerLink]="['/store', tenant(), 'products']">Continue shopping</a>
      </div>
    </ng-template>
  `,
  styles: [
    `
      .cart {
        padding: 2rem 1.5rem 3rem;
        display: flex;
        flex-direction: column;
        gap: 2rem;
      }

      h1 {
        margin: 0;
        font-size: 2rem;
      }

      .cart-content {
        display: grid;
        gap: 2rem;
        grid-template-columns: minmax(0, 3fr) minmax(260px, 1fr);
      }

      .cart-items {
        display: flex;
        flex-direction: column;
        gap: 1.25rem;
      }

      .cart-item {
        display: grid;
        gap: 1.5rem;
        grid-template-columns: 120px minmax(0, 1fr) auto;
        padding: 1.25rem;
        border-radius: 1rem;
        border: 1px solid #e0e0e0;
        background: #fff;
        align-items: center;
      }

      .item-media img {
        width: 120px;
        height: 120px;
        object-fit: cover;
        border-radius: 0.75rem;
      }

      .item-details {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      .item-details h2 {
        margin: 0;
        font-size: 1.125rem;
      }

      .variant {
        color: rgba(0, 0, 0, 0.54);
      }

      .price {
        font-weight: 600;
      }

      .item-actions {
        display: flex;
        gap: 0.75rem;
        align-items: center;
      }

      .quantity-label {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        font-weight: 500;
      }

      .quantity-label input {
        width: 72px;
        padding: 0.25rem 0.5rem;
        border: 1px solid #cfd8dc;
        border-radius: 0.5rem;
      }

      button.remove {
        background: none;
        border: none;
        color: #b00020;
        cursor: pointer;
      }

      .line-total {
        font-weight: 600;
      }

      .summary {
        border: 1px solid #e0e0e0;
        border-radius: 1rem;
        padding: 1.5rem;
        background: #fafafa;
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
      }

      .summary h2 {
        margin: 0;
        font-size: 1.25rem;
      }

      .summary dl {
        margin: 0;
        display: grid;
        gap: 0.75rem;
      }

      .summary div {
        display: flex;
        justify-content: space-between;
        font-size: 0.95rem;
      }

      .summary div.total {
        font-weight: 600;
        font-size: 1.05rem;
      }

      .checkout {
        display: inline-flex;
        justify-content: center;
        align-items: center;
        padding: 0.85rem 1.5rem;
        border-radius: 999px;
        background: #1976d2;
        color: #fff;
        text-decoration: none;
        font-weight: 600;
      }

      .loading,
      .empty {
        padding: 3rem 1.5rem;
        text-align: center;
      }

      .cart-error {
        margin: 0;
        color: #c62828;
      }

      .empty a {
        color: #1976d2;
        text-decoration: none;
        font-weight: 600;
      }

      @media (max-width: 960px) {
        .cart-content {
          grid-template-columns: 1fr;
        }

        .cart-item {
          grid-template-columns: minmax(0, 1fr);
          text-align: left;
        }

        .line-total {
          justify-self: start;
        }
      }
    `
  ]
})
export class CartComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly cartService = inject(CartService);
  private readonly tenantStore = inject(TenantStoreService);

  readonly tenant: Signal<string> = toSignal(
    this.route.paramMap.pipe(map(params => params.get('tenant') ?? '')),
    { initialValue: '' }
  );

  readonly cart = this.cartService.cart;
  readonly loading = this.cartService.loading;
  readonly error = this.cartService.error;

  readonly totalItems = computed(() =>
    this.cart()?.items.reduce((total, item) => total + item.quantity, 0) ?? 0
  );

  constructor() {
    effect(
      () => {
        const tenant = this.tenant();
        if (!tenant) {
          return;
        }

        this.tenantStore.setTenant(tenant);
        this.cartService.loadCart(tenant).subscribe({ error: () => {} });
      },
      { allowSignalWrites: true }
    );
  }

  trackByItemId(_: number, item: CartItem): string {
    return item.id;
  }

  onQuantityChange(item: CartItem, value: number | null): void {
    const tenant = this.tenant();
    if (!tenant || !item.id) {
      return;
    }

    const quantity = Math.max(1, Number(value || item.quantity));
    if (quantity === item.quantity) {
      return;
    }

    this.cartService.updateItemQuantity(tenant, item.id, quantity).subscribe();
  }

  removeItem(item: CartItem): void {
    const tenant = this.tenant();
    if (!tenant) {
      return;
    }

    this.cartService.removeItem(tenant, item.id).subscribe();
  }
}
