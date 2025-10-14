import { CommonModule } from '@angular/common';
import { Component, DestroyRef, Signal, computed, effect, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { OrderDetail, OrderService, OrderSummary } from '../../services/order.service';
import { TenantStoreService } from '../../services/tenant-store.service';

@Component({
  selector: 'app-order-history',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  template: `
    <div class="order-history">
      <h1>Order history</h1>
      <p class="subtitle">Look up your orders using the email address used at checkout.</p>

      <form [formGroup]="lookupForm" (ngSubmit)="lookup()" class="lookup-form">
        <label for="email">Email address</label>
        <div class="lookup-input">
          <input id="email" type="email" formControlName="email" placeholder="you@example.com" />
          <button type="submit" [disabled]="lookupForm.invalid || loading()">Search</button>
        </div>
        <p class="error" *ngIf="emailInvalid">Please provide a valid email address.</p>
      </form>

      <section class="results" *ngIf="orders().length; else noResults">
        <div class="orders">
          <article class="order" *ngFor="let order of orders()" (click)="viewOrder(order)" [class.active]="order.id === selectedOrder()?.id">
            <div>
              <h2>#{{ order.number }}</h2>
              <p>{{ order.placedAt | date: 'medium' }}</p>
            </div>
            <div class="meta">
              <span class="status">{{ order.status | titlecase }}</span>
              <span class="total">{{ order.total | currency: order.currency }}</span>
            </div>
          </article>
        </div>

        <aside class="details" *ngIf="selectedOrder() as detail">
          <h2>Order details</h2>
          <p class="detail-status">Status: {{ detail.status | titlecase }}</p>
          <p>Payment: {{ detail.paymentStatus | titlecase }}</p>

          <section class="detail-section">
            <h3>Items</h3>
            <div class="detail-items">
              <div class="detail-item" *ngFor="let item of detail.items">
                <div>
                  <p class="item-name">{{ item.name }}</p>
                  <p class="item-variant" *ngIf="item.variantName">Variant: {{ item.variantName }}</p>
                </div>
                <div class="item-meta">
                  <span>x{{ item.quantity }}</span>
                  <span>{{ item.total | currency: item.currency }}</span>
                </div>
              </div>
            </div>
          </section>

          <section class="detail-section" *ngIf="detail.shippingAddress">
            <h3>Shipping</h3>
            <address>
              <strong>{{ detail.shippingAddress?.fullName }}</strong>
              <span>{{ detail.shippingAddress?.addressLine1 }}</span>
              <span *ngIf="detail.shippingAddress?.addressLine2">{{ detail.shippingAddress?.addressLine2 }}</span>
              <span>
                {{ detail.shippingAddress?.city }},
                {{ detail.shippingAddress?.region }}
                {{ detail.shippingAddress?.postalCode }}
              </span>
              <span>{{ detail.shippingAddress?.country }}</span>
            </address>
          </section>

          <section class="detail-section">
            <h3>Totals</h3>
            <dl>
              <div>
                <dt>Subtotal</dt>
                <dd>{{ detail.subtotal | currency: detail.currency }}</dd>
              </div>
              <div>
                <dt>Shipping</dt>
                <dd>{{ detail.shippingTotal | currency: detail.currency }}</dd>
              </div>
              <div>
                <dt>Taxes</dt>
                <dd>{{ detail.taxTotal | currency: detail.currency }}</dd>
              </div>
              <div class="grand">
                <dt>Total</dt>
                <dd>{{ detail.total | currency: detail.currency }}</dd>
              </div>
            </dl>
          </section>
        </aside>
      </section>
    </div>

    <ng-template #noResults>
      <div class="empty">
        <p *ngIf="!loading() && !orders().length">Enter an email to see orders for this tenant.</p>
        <p *ngIf="loading()">Loading orders…</p>
      </div>
    </ng-template>
  `,
  styles: [
    `
      .order-history {
        padding: 2rem 1.5rem 3rem;
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
      }

      h1 {
        margin: 0;
        font-size: 2rem;
      }

      .subtitle {
        margin: 0;
        color: rgba(0, 0, 0, 0.6);
      }

      .lookup-form {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
        max-width: 420px;
      }

      .lookup-input {
        display: flex;
        gap: 0.75rem;
      }

      .lookup-input input {
        flex: 1;
        border: 1px solid #cfd8dc;
        border-radius: 0.75rem;
        padding: 0.75rem 1rem;
      }

      .lookup-input button {
        padding: 0.75rem 1.25rem;
        border-radius: 0.75rem;
        background: #1976d2;
        color: #fff;
        border: none;
        font-weight: 600;
        cursor: pointer;
      }

      .lookup-input button:disabled {
        opacity: 0.65;
        cursor: not-allowed;
      }

      .error {
        margin: 0;
        color: #c62828;
      }

      .results {
        display: grid;
        gap: 1.5rem;
        grid-template-columns: minmax(240px, 1fr) minmax(0, 2fr);
      }

      .orders {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .order {
        border: 1px solid #e0e0e0;
        border-radius: 0.75rem;
        padding: 1rem 1.25rem;
        cursor: pointer;
        display: flex;
        justify-content: space-between;
        gap: 1rem;
        background: #fff;
        transition: border-color 0.2s ease, box-shadow 0.2s ease;
      }

      .order.active {
        border-color: #1976d2;
        box-shadow: 0 12px 24px rgba(25, 118, 210, 0.12);
      }

      .order h2 {
        margin: 0 0 0.5rem;
        font-size: 1.1rem;
      }

      .order .meta {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        text-align: right;
      }

      .order .status {
        font-weight: 600;
      }

      .order .total {
        font-weight: 600;
      }

      .details {
        border: 1px solid #e0e0e0;
        border-radius: 0.75rem;
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
        background: #fff;
      }

      .details h2 {
        margin: 0;
      }

      .detail-section {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
      }

      .detail-items {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
      }

      .detail-item {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
      }

      .detail-item .item-name {
        margin: 0;
        font-weight: 600;
      }

      .detail-item .item-variant {
        margin: 0;
        color: rgba(0, 0, 0, 0.6);
      }

      .detail-item .item-meta {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        align-items: flex-end;
        font-weight: 600;
      }

      address {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        font-style: normal;
      }

      dl {
        margin: 0;
        display: grid;
        gap: 0.5rem;
      }

      dl div {
        display: flex;
        justify-content: space-between;
      }

      dl div.grand {
        font-weight: 600;
      }

      .empty {
        padding: 2rem 1rem;
        text-align: center;
        border: 1px dashed #cfd8dc;
        border-radius: 0.75rem;
      }

      @media (max-width: 960px) {
        .results {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class OrderHistoryComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(FormBuilder);
  private readonly orderService = inject(OrderService);
  private readonly tenantStore = inject(TenantStoreService);
  private readonly destroyRef = inject(DestroyRef);

  readonly tenant: Signal<string> = toSignal(
    this.route.paramMap.pipe(map(params => params.get('tenant') ?? '')),
    { initialValue: '' }
  );

  readonly lookupForm = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]]
  });

  private readonly ordersSignal = signal<OrderSummary[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly selectedOrderSignal = signal<OrderDetail | null>(null);

  readonly orders = this.ordersSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly selectedOrder = this.selectedOrderSignal.asReadonly();

  readonly emailInvalid = computed(
    () =>
      !!this.lookupForm.get('email') &&
      this.lookupForm.get('email')!.invalid &&
      (this.lookupForm.get('email')!.dirty || this.lookupForm.get('email')!.touched)
  );

  constructor() {
    effect(
      () => {
        const tenant = this.tenant();
        if (!tenant) {
          return;
        }

        this.tenantStore.setTenant(tenant);
      },
      { allowSignalWrites: true }
    );

    this.destroyRef.onDestroy(() => {
      this.ordersSignal.set([]);
      this.selectedOrderSignal.set(null);
    });
  }

  lookup(): void {
    if (this.lookupForm.invalid) {
      this.lookupForm.markAllAsTouched();
      return;
    }

    const tenant = this.tenant();
    const email = this.lookupForm.value.email!;
    if (!tenant || !email) {
      return;
    }

    this.loadingSignal.set(true);
    this.orderService.getOrderHistory(tenant, email).subscribe({
      next: orders => {
        this.ordersSignal.set(orders);
        this.loadingSignal.set(false);
        if (orders.length) {
          this.viewOrder(orders[0]);
        } else {
          this.selectedOrderSignal.set(null);
        }
      },
      error: () => {
        this.loadingSignal.set(false);
        this.ordersSignal.set([]);
      }
    });
  }

  viewOrder(order: OrderSummary): void {
    const tenant = this.tenant();
    if (!tenant) {
      return;
    }

    this.loadingSignal.set(true);
    this.orderService.getOrderDetail(tenant, order.id).subscribe({
      next: detail => {
        this.selectedOrderSignal.set(detail);
        this.loadingSignal.set(false);
      },
      error: () => {
        this.loadingSignal.set(false);
      }
    });
  }
}
