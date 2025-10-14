import { CommonModule } from '@angular/common';
import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { toSignal } from '@angular/core/rxjs-interop';
import { startWith } from 'rxjs/operators';
import {
  AdminOrderDetail,
  AdminOrderService,
  AdminOrderStatus,
  AdminOrderSummary
} from '../../services/admin-order.service';

const ORDER_STATUSES: AdminOrderStatus[] = [
  'pending',
  'processing',
  'fulfilled',
  'shipped',
  'delivered',
  'cancelled',
  'refunded'
];

@Component({
  selector: 'app-admin-order-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  template: `
    <div class="orders-page">
      <header class="page-header">
        <div>
          <h1>Orders</h1>
          <p class="subtitle">Monitor and manage orders across all tenants.</p>
        </div>

        <div class="filters">
          <label for="statusFilter">Status</label>
          <select id="statusFilter" [formControl]="statusFilterControl">
            <option value="">All statuses</option>
            <option *ngFor="let status of statuses" [value]="status">{{ status | titlecase }}</option>
          </select>
        </div>
      </header>

      <div class="content">
        <section class="orders" *ngIf="orders().length; else emptyState">
          <table>
            <thead>
              <tr>
                <th>Order</th>
                <th>Tenant</th>
                <th>Customer</th>
                <th>Status</th>
                <th>Payment</th>
                <th>Total</th>
                <th>Placed</th>
              </tr>
            </thead>
            <tbody>
              <tr
                *ngFor="let order of orders(); trackBy: trackByOrderId"
                (click)="viewOrder(order)"
                [class.active]="order.id === selectedOrder()?.id"
              >
                <td>#{{ order.number }}</td>
                <td>{{ order.tenantName }}</td>
                <td>{{ order.customerEmail }}</td>
                <td>
                  <select [value]="order.status" (change)="onStatusChange(order, $any($event.target).value)">
                    <option *ngFor="let status of statuses" [value]="status">{{ status | titlecase }}</option>
                  </select>
                </td>
                <td>{{ order.paymentStatus | titlecase }}</td>
                <td>{{ order.total | currency: order.currency }}</td>
                <td>{{ order.placedAt | date: 'short' }}</td>
              </tr>
            </tbody>
          </table>
        </section>

        <aside class="details" *ngIf="selectedOrder() as order">
          <h2>Order #{{ order.number }}</h2>
          <p class="detail-subtitle">
            {{ order.tenantName }} • {{ order.customerEmail }} • {{ order.placedAt | date: 'medium' }}
          </p>

          <dl class="detail-stats">
            <div>
              <dt>Status</dt>
              <dd>{{ order.status | titlecase }}</dd>
            </div>
            <div>
              <dt>Payment</dt>
              <dd>{{ order.paymentStatus | titlecase }}</dd>
            </div>
            <div>
              <dt>Total</dt>
              <dd>{{ order.total | currency: order.currency }}</dd>
            </div>
          </dl>

          <section class="detail-section">
            <h3>Items</h3>
            <div class="detail-items">
              <div class="detail-item" *ngFor="let item of order.items">
                <div>
                  <p class="name">{{ item.name }}</p>
                  <p class="quantity">Qty {{ item.quantity }}</p>
                </div>
                <div class="amounts">
                  <span>{{ item.unitPrice | currency: order.currency }} ea</span>
                  <strong>{{ item.total | currency: order.currency }}</strong>
                </div>
              </div>
            </div>
          </section>

          <section class="detail-section" *ngIf="order.shippingAddress">
            <h3>Shipping address</h3>
            <address>
              <strong>{{ order.shippingAddress?.fullName }}</strong>
              <span>{{ order.shippingAddress?.addressLine1 }}</span>
              <span *ngIf="order.shippingAddress?.addressLine2">{{ order.shippingAddress?.addressLine2 }}</span>
              <span>
                {{ order.shippingAddress?.city }},
                {{ order.shippingAddress?.region }}
                {{ order.shippingAddress?.postalCode }}
              </span>
              <span>{{ order.shippingAddress?.country }}</span>
            </address>
          </section>

          <section class="detail-section">
            <h3>Charges</h3>
            <dl>
              <div>
                <dt>Subtotal</dt>
                <dd>{{ order.subtotal | currency: order.currency }}</dd>
              </div>
              <div>
                <dt>Shipping</dt>
                <dd>{{ order.shippingTotal | currency: order.currency }}</dd>
              </div>
              <div>
                <dt>Taxes</dt>
                <dd>{{ order.taxTotal | currency: order.currency }}</dd>
              </div>
            </dl>
          </section>
        </aside>
      </div>
    </div>

    <ng-template #emptyState>
      <div class="empty">
        <p *ngIf="!loading()">No orders found for the selected filter.</p>
        <p *ngIf="loading()">Loading orders…</p>
      </div>
    </ng-template>
  `,
  styles: [
    `
      .orders-page {
        display: flex;
        flex-direction: column;
        gap: 2rem;
      }

      .page-header {
        display: flex;
        justify-content: space-between;
        align-items: flex-end;
      }

      .page-header h1 {
        margin: 0;
        font-size: 2rem;
      }

      .subtitle {
        margin: 0.25rem 0 0;
        color: rgba(0, 0, 0, 0.54);
      }

      .filters {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        align-items: flex-start;
      }

      select {
        border: 1px solid #cfd8dc;
        border-radius: 0.75rem;
        padding: 0.5rem 0.75rem;
      }

      .content {
        display: grid;
        gap: 2rem;
        grid-template-columns: minmax(0, 2fr) minmax(260px, 1fr);
        align-items: start;
      }

      table {
        width: 100%;
        border-collapse: collapse;
        background: #fff;
        border-radius: 1rem;
        overflow: hidden;
        border: 1px solid #e0e0e0;
      }

      th,
      td {
        text-align: left;
        padding: 0.85rem 1rem;
        border-bottom: 1px solid #e0e0e0;
      }

      tbody tr {
        cursor: pointer;
        transition: background-color 0.2s ease;
      }

      tbody tr:hover {
        background: rgba(25, 118, 210, 0.08);
      }

      tbody tr.active {
        background: rgba(25, 118, 210, 0.16);
      }

      tbody tr:last-child td {
        border-bottom: none;
      }

      .details {
        border: 1px solid #e0e0e0;
        border-radius: 1rem;
        padding: 1.5rem;
        background: #fff;
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
        position: sticky;
        top: 1rem;
      }

      .detail-subtitle {
        margin: 0;
        color: rgba(0, 0, 0, 0.54);
      }

      .detail-stats {
        display: grid;
        gap: 0.75rem;
        margin: 0;
      }

      .detail-stats div {
        display: flex;
        justify-content: space-between;
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

      .detail-item .name {
        margin: 0;
        font-weight: 600;
      }

      .detail-item .quantity {
        margin: 0;
        color: rgba(0, 0, 0, 0.54);
      }

      .detail-item .amounts {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        align-items: flex-end;
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

      .empty {
        border: 1px dashed #cfd8dc;
        border-radius: 1rem;
        padding: 2rem 1rem;
        text-align: center;
        background: #fff;
      }

      @media (max-width: 960px) {
        .content {
          grid-template-columns: 1fr;
        }

        .details {
          position: static;
        }
      }
    `
  ]
})
export class OrderListComponent {
  private readonly orderService = inject(AdminOrderService);
  private readonly destroyRef = inject(DestroyRef);

  readonly statusFilterControl = new FormControl<string>('');
  private readonly statusFilter = toSignal(
    this.statusFilterControl.valueChanges.pipe(startWith(this.statusFilterControl.value)),
    { initialValue: this.statusFilterControl.value }
  );

  private readonly ordersSignal = signal<AdminOrderSummary[]>([]);
  private readonly loadingSignal = signal(false);
  private readonly selectedOrderSignal = signal<AdminOrderDetail | null>(null);

  readonly orders = this.ordersSignal.asReadonly();
  readonly loading = this.loadingSignal.asReadonly();
  readonly selectedOrder = this.selectedOrderSignal.asReadonly();

  readonly statuses = ORDER_STATUSES;

  constructor() {
    effect(
      () => {
        const status = this.statusFilter();
        this.loadOrders(status || undefined);
      },
      { allowSignalWrites: true }
    );

    this.destroyRef.onDestroy(() => {
      this.ordersSignal.set([]);
      this.selectedOrderSignal.set(null);
    });
  }

  trackByOrderId(_: number, order: AdminOrderSummary): string {
    return order.id;
  }

  viewOrder(order: AdminOrderSummary): void {
    this.loadingSignal.set(true);
    this.orderService.getOrder(order.id).subscribe({
      next: detail => {
        this.selectedOrderSignal.set(detail);
        this.loadingSignal.set(false);
      },
      error: () => {
        this.loadingSignal.set(false);
      }
    });
  }

  onStatusChange(order: AdminOrderSummary, status: AdminOrderStatus): void {
    if (order.status === status) {
      return;
    }

    this.loadingSignal.set(true);
    this.orderService.updateStatus(order.id, status).subscribe({
      next: updated => {
        this.ordersSignal.update(orders =>
          orders.map(item => (item.id === order.id ? { ...item, status: updated.status } : item))
        );
        this.selectedOrderSignal.set(updated);
        this.loadingSignal.set(false);
      },
      error: () => {
        this.loadingSignal.set(false);
      }
    });
  }

  private loadOrders(status?: string): void {
    this.loadingSignal.set(true);
    this.orderService.getOrders({ status: status || undefined }).subscribe({
      next: response => {
        this.ordersSignal.set(response.items);
        this.loadingSignal.set(false);
        if (response.items.length) {
          const selected = this.selectedOrderSignal();
          if (!selected || !response.items.some(item => item.id === selected.id)) {
            this.viewOrder(response.items[0]);
          }
        } else {
          this.selectedOrderSignal.set(null);
        }
      },
      error: () => {
        this.loadingSignal.set(false);
        this.ordersSignal.set([]);
        this.selectedOrderSignal.set(null);
      }
    });
  }
}
