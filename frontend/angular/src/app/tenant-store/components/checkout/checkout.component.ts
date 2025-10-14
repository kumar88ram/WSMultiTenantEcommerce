import { CommonModule } from '@angular/common';
import { Component, DestroyRef, Signal, computed, effect, inject, signal } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { CartService } from '../../services/cart.service';
import {
  CheckoutConfiguration,
  CheckoutService,
  CreateCheckoutSessionRequest,
  PaymentMethodOption
} from '../../services/checkout.service';
import { TenantStoreService } from '../../services/tenant-store.service';
import { OrderDetail, OrderService } from '../../services/order.service';
import { CardPaymentDetails, PaymentFlowService } from '../../services/payment-flow.service';

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, RouterModule, ReactiveFormsModule],
  template: `
    <div class="checkout" *ngIf="cartEmpty(); else checkoutContent">
      <div class="empty">
        <h1>Your cart is empty</h1>
        <a [routerLink]="['/store', tenant(), 'products']">Continue shopping</a>
      </div>
    </div>

    <ng-template #checkoutContent>
      <div class="checkout" *ngIf="cartService.cart() as currentCart; else cartLoading">
        <h1>Checkout</h1>

        <section class="payment-status" *ngIf="paymentStatusMessage()">
          <p>{{ paymentStatusMessage() }}</p>
        </section>

        <section class="confirmation" *ngIf="orderConfirmation() as confirmation">
          <h2>Order {{ confirmation.number }}</h2>
          <p>Status: {{ confirmation.status | titlecase }}</p>
          <p>Total: {{ confirmation.total | currency: confirmation.currency }}</p>

          <div class="confirmation-items">
            <div class="confirmation-item" *ngFor="let item of confirmation.items">
              <div>
                <h3>{{ item.name }}</h3>
                <p *ngIf="item.variantName">Variant: {{ item.variantName }}</p>
              </div>
              <div class="confirmation-item__meta">
                <span>x{{ item.quantity }}</span>
                <span>{{ item.total | currency: item.currency }}</span>
              </div>
            </div>
          </div>

          <p class="webhook-note">
            Payment states are finalized asynchronously by the platform webhook handler. You can safely
            close this tab once you receive the confirmation email.
          </p>
        </section>

        <div class="checkout-grid">
          <form [formGroup]="checkoutForm" (ngSubmit)="submit()" novalidate>
            <fieldset formGroupName="shippingAddress">
              <legend>Shipping address</legend>

              <div class="field">
                <label for="fullName">Full name</label>
                <input id="fullName" type="text" formControlName="fullName" required />
                <p class="error" *ngIf="hasError('fullName')">Please enter the recipient name.</p>
              </div>

              <div class="field">
                <label for="addressLine1">Address line 1</label>
                <input id="addressLine1" type="text" formControlName="addressLine1" required />
                <p class="error" *ngIf="hasError('addressLine1')">Please enter the street address.</p>
              </div>

              <div class="field">
                <label for="addressLine2">Address line 2</label>
                <input id="addressLine2" type="text" formControlName="addressLine2" />
              </div>

              <div class="field-grid">
                <div class="field">
                  <label for="city">City</label>
                  <input id="city" type="text" formControlName="city" required />
                  <p class="error" *ngIf="hasError('city')">Please enter the city.</p>
                </div>

                <div class="field">
                  <label for="region">State / Region</label>
                  <input id="region" type="text" formControlName="region" />
                </div>
              </div>

              <div class="field-grid">
                <div class="field">
                  <label for="postalCode">Postal code</label>
                  <input id="postalCode" type="text" formControlName="postalCode" required />
                  <p class="error" *ngIf="hasError('postalCode')">Please provide a postal code.</p>
                </div>

                <div class="field">
                  <label for="country">Country</label>
                  <input id="country" type="text" formControlName="country" required />
                  <p class="error" *ngIf="hasError('country')">Please provide a country.</p>
                </div>
              </div>

              <div class="field-grid">
                <div class="field">
                  <label for="email">Email</label>
                  <input id="email" type="email" formControlName="email" required />
                  <p class="error" *ngIf="hasError('email')">A valid email is required.</p>
                </div>

                <div class="field">
                  <label for="phone">Phone</label>
                  <input id="phone" type="tel" formControlName="phone" />
                </div>
              </div>
            </fieldset>

            <fieldset>
              <legend>Shipping method</legend>
              <div class="options" *ngIf="checkoutConfig()?.shippingMethods.length; else noShipping">
                <label class="option" *ngFor="let method of checkoutConfig()?.shippingMethods">
                  <input type="radio" formControlName="shippingMethodId" [value]="method.id" />
                  <div>
                    <div class="option-header">
                      <span class="name">{{ method.name }}</span>
                      <span class="price">{{ method.amount | currency: method.currency }}</span>
                    </div>
                    <p class="description" *ngIf="method.description">{{ method.description }}</p>
                    <p class="eta" *ngIf="method.estimatedDaysMin">
                      Estimated {{ method.estimatedDaysMin }}–{{ method.estimatedDaysMax ?? method.estimatedDaysMin }}
                      business days
                    </p>
                  </div>
                </label>
              </div>
            </fieldset>

            <div class="field coupon-field">
              <label for="couponCode">Coupon code</label>
              <input id="couponCode" type="text" formControlName="couponCode" placeholder="Enter a code" />
            </div>

            <fieldset>
              <legend>Payment method</legend>
              <div class="options" *ngIf="checkoutConfig()?.paymentMethods.length; else noPayment">
                <label class="option" *ngFor="let method of checkoutConfig()?.paymentMethods">
                  <input type="radio" formControlName="paymentMethodId" [value]="method.id" />
                  <div>
                    <div class="option-header">
                      <span class="name">{{ method.name }}</span>
                      <span class="provider">{{ method.provider }}</span>
                      <span class="flow" *ngIf="method.flow === 'inline_card'">In-page</span>
                      <span class="flow" *ngIf="method.flow === 'hosted_redirect'">Redirect</span>
                    </div>
                    <p class="description" *ngIf="method.instructions">{{ method.instructions }}</p>
                  </div>
                </label>
              </div>
            </fieldset>

            <fieldset
              class="card-entry"
              formGroupName="payment"
              *ngIf="selectedPaymentMethod()?.flow === 'inline_card'"
            >
              <legend>Card details</legend>

              <div class="field">
                <label for="cardholderName">Name on card</label>
                <input id="cardholderName" type="text" formControlName="cardholderName" />
                <p class="error" *ngIf="hasCardError('cardholderName')">Please enter the name on the card.</p>
              </div>

              <div class="field">
                <label for="cardNumber">Card number</label>
                <input id="cardNumber" type="text" inputmode="numeric" autocomplete="cc-number" formControlName="cardNumber" />
                <p class="error" *ngIf="hasCardError('cardNumber')">Please enter a valid card number.</p>
              </div>

              <div class="field-grid">
                <div class="field">
                  <label for="expiryMonth">Expiry month</label>
                  <input id="expiryMonth" type="text" inputmode="numeric" maxlength="2" formControlName="expiryMonth" />
                  <p class="error" *ngIf="hasCardError('expiryMonth')">Enter MM.</p>
                </div>
                <div class="field">
                  <label for="expiryYear">Expiry year</label>
                  <input id="expiryYear" type="text" inputmode="numeric" maxlength="4" formControlName="expiryYear" />
                  <p class="error" *ngIf="hasCardError('expiryYear')">Enter YYYY.</p>
                </div>
                <div class="field">
                  <label for="cvc">CVC</label>
                  <input id="cvc" type="text" inputmode="numeric" maxlength="4" formControlName="cvc" />
                  <p class="error" *ngIf="hasCardError('cvc')">Enter the security code.</p>
                </div>
              </div>
            </fieldset>

            <button type="submit" class="submit" [disabled]="checkoutForm.invalid || submitting()">
              {{ submitting() ? 'Processing…' : 'Pay now' }}
            </button>
          </form>

          <aside class="summary">
            <h2>Order summary</h2>
            <div class="items">
              <article class="item" *ngFor="let item of currentCart.items">
                <div>
                  <h3>{{ item.name }}</h3>
                  <p *ngIf="item.metadata?.variantName">Variant: {{ item.metadata?.variantName }}</p>
                  <p class="unit">{{ item.unitPrice | currency: item.currency }} × {{ item.quantity }}</p>
                </div>
                <span class="line">{{ item.lineTotal | currency: item.currency }}</span>
              </article>
            </div>

            <dl>
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
          </aside>
        </div>
      </div>
    </ng-template>

    <ng-template #noShipping>
      <p class="empty">No shipping methods are configured for this tenant.</p>
    </ng-template>

    <ng-template #noPayment>
      <p class="empty">No payment methods are currently available.</p>
    </ng-template>

    <ng-template #cartLoading>
      <div class="empty">
        <p>Loading your cart…</p>
      </div>
    </ng-template>
  `,
  styles: [
    `
      .checkout {
        padding: 2rem 1.5rem 3rem;
        display: flex;
        flex-direction: column;
        gap: 2rem;
      }

      h1 {
        margin: 0;
        font-size: 2rem;
      }

      form {
        display: flex;
        flex-direction: column;
        gap: 2rem;
      }

      fieldset {
        border: 1px solid #e0e0e0;
        border-radius: 1rem;
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
        background: #fff;
      }

      legend {
        font-weight: 600;
        padding: 0 0.5rem;
      }

      .field,
      .field-grid {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      .field-grid {
        flex-direction: row;
        gap: 1rem;
      }

      .field-grid .field {
        flex: 1;
      }

      label {
        font-weight: 500;
      }

      input[type='text'],
      input[type='email'],
      input[type='tel'] {
        border: 1px solid #cfd8dc;
        border-radius: 0.75rem;
        padding: 0.75rem 1rem;
        font-size: 1rem;
        width: 100%;
      }

      .coupon-field {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      .error {
        color: #c62828;
        font-size: 0.85rem;
        margin: 0;
      }

      .options {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .option {
        display: grid;
        grid-template-columns: auto 1fr;
        gap: 1rem;
        padding: 1rem;
        border: 1px solid #e0e0e0;
        border-radius: 0.75rem;
        cursor: pointer;
        background: #fafafa;
      }

      .option input {
        margin-top: 0.35rem;
      }

      .option-header {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
      }

      .option-header .name {
        font-weight: 600;
      }

      .option-header .price,
      .option-header .provider {
        font-weight: 500;
        color: rgba(0, 0, 0, 0.7);
      }

      .option-header .flow {
        margin-left: auto;
        margin-right: 0;
        font-size: 0.75rem;
        padding: 0.1rem 0.75rem;
        border-radius: 999px;
        background: #e3f2fd;
        color: #0d47a1;
        font-weight: 600;
        text-transform: uppercase;
      }

      .description,
      .eta {
        margin: 0.25rem 0 0;
        color: rgba(0, 0, 0, 0.6);
      }

      .submit {
        align-self: flex-start;
        padding: 0.85rem 1.75rem;
        background: #1976d2;
        color: #fff;
        border-radius: 999px;
        border: none;
        font-weight: 600;
        cursor: pointer;
      }

      .submit:disabled {
        opacity: 0.65;
        cursor: not-allowed;
      }

      .checkout-grid {
        display: grid;
        gap: 2rem;
        grid-template-columns: minmax(0, 2fr) minmax(280px, 1fr);
      }

      .summary {
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
        border: 1px solid #e0e0e0;
        border-radius: 1rem;
        padding: 1.5rem;
        background: #fff;
      }

      .summary h2 {
        margin: 0;
        font-size: 1.5rem;
      }

      .summary .items {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .summary .item {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
      }

      .summary .item h3 {
        margin: 0 0 0.25rem;
        font-size: 1rem;
      }

      .summary .item .unit {
        color: rgba(0, 0, 0, 0.6);
      }

      .summary dl {
        margin: 0;
        display: grid;
        gap: 0.75rem;
      }

      .summary dl div {
        display: flex;
        justify-content: space-between;
      }

      .summary dl div.total {
        font-weight: 600;
      }

      .empty {
        text-align: center;
      }

      .empty a {
        color: #1976d2;
        text-decoration: none;
        font-weight: 600;
      }

      .payment-status,
      .confirmation {
        border: 1px solid #e0e0e0;
        border-radius: 1rem;
        background: #f8fbff;
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .confirmation-items {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .confirmation-item {
        display: flex;
        justify-content: space-between;
        gap: 1rem;
      }

      .confirmation-item h3 {
        margin: 0;
      }

      .confirmation-item__meta {
        text-align: right;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        font-weight: 600;
      }

      .webhook-note {
        margin: 0;
        color: rgba(0, 0, 0, 0.6);
        font-size: 0.95rem;
      }

      @media (max-width: 960px) {
        .checkout-grid {
          grid-template-columns: 1fr;
        }

        .field-grid {
          flex-direction: column;
        }
      }
    `
  ]
})
export class CheckoutComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly cartService = inject(CartService);
  private readonly checkoutService = inject(CheckoutService);
  private readonly paymentFlowService = inject(PaymentFlowService);
  private readonly orderService = inject(OrderService);
  private readonly tenantStore = inject(TenantStoreService);
  private readonly formBuilder = inject(FormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly tenant: Signal<string> = toSignal(
    this.route.paramMap.pipe(map(params => params.get('tenant') ?? '')),
    { initialValue: '' }
  );

  private readonly orderIdFromReturn: Signal<string | null> = toSignal(
    this.route.queryParamMap.pipe(map(params => params.get('orderId'))),
    { initialValue: null }
  );

  private readonly returnStatus: Signal<string | null> = toSignal(
    this.route.queryParamMap.pipe(map(params => params.get('paymentStatus'))),
    { initialValue: null }
  );

  private readonly configSignal = signal<CheckoutConfiguration | null>(null);
  private readonly submittingSignal = signal(false);
  private readonly paymentStatusMessageSignal = signal<string | null>(null);
  private readonly orderConfirmationSignal = signal<OrderDetail | null>(null);

  readonly checkoutConfig = this.configSignal.asReadonly();
  readonly submitting = this.submittingSignal.asReadonly();
  readonly paymentStatusMessage = this.paymentStatusMessageSignal.asReadonly();
  readonly orderConfirmation = this.orderConfirmationSignal.asReadonly();

  readonly cartEmpty = computed(() => {
    if (this.cartService.loading()) {
      return false;
    }

    return !(this.cartService.cart()?.items?.length);
  });

  readonly checkoutForm = this.formBuilder.group({
    shippingAddress: this.formBuilder.group({
      fullName: ['', Validators.required],
      addressLine1: ['', Validators.required],
      addressLine2: [''],
      city: ['', Validators.required],
      region: [''],
      postalCode: ['', Validators.required],
      country: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      phone: ['']
    }),
    shippingMethodId: ['', Validators.required],
    paymentMethodId: ['', Validators.required],
    couponCode: [''],
    payment: this.formBuilder.group({
      cardholderName: [''],
      cardNumber: [''],
      expiryMonth: [''],
      expiryYear: [''],
      cvc: ['']
    })
  });

  readonly selectedPaymentMethod = computed<PaymentMethodOption | null>(() => {
    const config = this.checkoutConfig();
    const selectedId = this.checkoutForm.get('paymentMethodId')?.value;
    if (!config || !selectedId) {
      return null;
    }

    return config.paymentMethods.find(method => method.id === selectedId) ?? null;
  });

  private processedReturn = false;

  constructor() {
    effect(
      () => {
        const tenant = this.tenant();
        if (!tenant) {
          return;
        }

        this.tenantStore.setTenant(tenant);
        this.cartService.loadCart(tenant).subscribe({ error: () => {} });
        this.fetchConfiguration(tenant);
      },
      { allowSignalWrites: true }
    );

    effect(
      () => {
        const config = this.checkoutConfig();
        if (!config) {
          return;
        }

        const form = this.checkoutForm;
        if (!form.get('shippingMethodId')?.value && config.shippingMethods[0]) {
          form.patchValue({ shippingMethodId: config.shippingMethods[0].id });
        }

        if (!form.get('paymentMethodId')?.value && config.paymentMethods[0]) {
          form.patchValue({ paymentMethodId: config.paymentMethods[0].id });
        }
      },
      { allowSignalWrites: true }
    );

    effect(
      () => {
        const method = this.selectedPaymentMethod();
        const paymentGroup = this.checkoutForm.get('payment') as FormGroup | null;
        if (!paymentGroup) {
          return;
        }

        const { cardholderName, cardNumber, expiryMonth, expiryYear, cvc } = paymentGroup.controls;
        if (method?.flow === 'inline_card') {
          cardholderName.setValidators([Validators.required]);
          cardNumber.setValidators([Validators.required, Validators.minLength(12)]);
          expiryMonth.setValidators([Validators.required, Validators.pattern(/^(0?[1-9]|1[0-2])$/)]);
          expiryYear.setValidators([Validators.required, Validators.pattern(/^[0-9]{2,4}$/)]);
          cvc.setValidators([Validators.required, Validators.minLength(3), Validators.maxLength(4)]);
        } else {
          cardholderName.clearValidators();
          cardNumber.clearValidators();
          expiryMonth.clearValidators();
          expiryYear.clearValidators();
          cvc.clearValidators();
        }

        Object.values(paymentGroup.controls).forEach(control =>
          control.updateValueAndValidity({ emitEvent: false })
        );
      },
      { allowSignalWrites: true }
    );

    effect(
      () => {
        const tenant = this.tenant();
        const orderId = this.orderIdFromReturn();
        const status = this.returnStatus();
        if (!tenant || !orderId || this.processedReturn) {
          return;
        }

        this.processedReturn = true;
        this.resolveReturn(tenant, orderId, status);
      },
      { allowSignalWrites: true }
    );

    this.destroyRef.onDestroy(() => {
      this.submittingSignal.set(false);
    });
  }

  submit(): void {
    if (this.checkoutForm.invalid) {
      this.checkoutForm.markAllAsTouched();
      return;
    }

    const tenant = this.tenant();
    const cart = this.cartService.cart();
    const method = this.selectedPaymentMethod();
    if (!tenant || !cart || !method) {
      return;
    }

    const paymentGroup = this.checkoutForm.get('payment') as FormGroup | null;
    if (method.flow === 'inline_card' && paymentGroup?.invalid) {
      paymentGroup.markAllAsTouched();
      return;
    }

    const shippingAddress = this.checkoutForm.value.shippingAddress!;
    const request: CreateCheckoutSessionRequest = {
      cartId: cart.id,
      shippingAddress: {
        fullName: shippingAddress.fullName!,
        addressLine1: shippingAddress.addressLine1!,
        addressLine2: shippingAddress.addressLine2 ?? undefined,
        city: shippingAddress.city!,
        region: shippingAddress.region ?? undefined,
        postalCode: shippingAddress.postalCode!,
        country: shippingAddress.country!,
        email: shippingAddress.email!,
        phone: shippingAddress.phone ?? undefined
      },
      shippingMethodId: this.checkoutForm.value.shippingMethodId!,
      paymentMethodId: this.checkoutForm.value.paymentMethodId!,
      returnUrl: this.buildReturnUrl(tenant),
      cancelUrl: this.buildCancelUrl(tenant),
      couponCode: this.checkoutForm.value.couponCode ?? undefined
    };

    let cardDetails: CardPaymentDetails | undefined;
    if (method.flow === 'inline_card' && paymentGroup) {
      const { cardholderName, cardNumber, expiryMonth, expiryYear, cvc } = paymentGroup.value;
      cardDetails = {
        cardholderName: cardholderName ?? '',
        cardNumber: (cardNumber ?? '').replace(/\s+/g, ''),
        expiryMonth: String(expiryMonth ?? ''),
        expiryYear: String(expiryYear ?? ''),
        cvc: String(cvc ?? '')
      };
    }

    this.submittingSignal.set(true);
    this.paymentFlowService
      .initiatePayment(tenant, request, method, cardDetails)
      .subscribe({
        next: session => {
          this.handleCheckoutSession(session);
        },
        error: () => {
          this.submittingSignal.set(false);
          this.paymentStatusMessageSignal.set('Unable to start the payment flow. Please try again.');
        }
      });
  }

  hasError(controlName: string): boolean {
    const control = this.checkoutForm.get(['shippingAddress', controlName]);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  hasCardError(controlName: string): boolean {
    const method = this.selectedPaymentMethod();
    if (method?.flow !== 'inline_card') {
      return false;
    }

    const group = this.checkoutForm.get('payment') as FormGroup | null;
    const control = group?.get(controlName);
    return !!control && control.invalid && (control.touched || control.dirty);
  }

  private fetchConfiguration(tenant: string): void {
    this.checkoutService.getConfiguration(tenant).subscribe({
      next: config => this.configSignal.set(config),
      error: () => this.configSignal.set({ shippingMethods: [], paymentMethods: [] })
    });
  }

  private handleCheckoutSession(session: {
    status: string;
    redirectUrl?: string;
    clientSecret?: string;
    orderId: string;
  }): void {
    if (session.redirectUrl) {
      if (typeof window !== 'undefined') {
        window.location.href = session.redirectUrl;
      }
      return;
    }

    this.submittingSignal.set(false);
    const tenant = this.tenant();
    if (!tenant) {
      return;
    }

    if (session.clientSecret || session.status === 'requires_client_confirmation' || session.status === 'processing') {
      this.paymentStatusMessageSignal.set('Processing your payment…');
      this.resolveReturn(tenant, session.orderId, 'success');
      return;
    }

    if (session.status === 'paid') {
      this.paymentStatusMessageSignal.set('Payment completed successfully.');
      this.resolveReturn(tenant, session.orderId, 'success');
    } else if (session.status === 'failed') {
      this.paymentStatusMessageSignal.set('Payment could not be completed. Please try another method.');
    } else {
      this.paymentStatusMessageSignal.set('Payment status is being verified.');
      this.resolveReturn(tenant, session.orderId, null);
    }
  }

  private resolveReturn(tenant: string, orderId: string, status: string | null): void {
    this.paymentStatusMessageSignal.set('Confirming your payment status…');
    this.paymentFlowService.handleReturn(tenant, orderId).subscribe({
      next: paymentStatus => {
        if (paymentStatus.status === 'paid' || status === 'success') {
          this.paymentStatusMessageSignal.set('Payment confirmed! Here is your order summary.');
          this.cartService.clearCart();
        } else if (
          paymentStatus.status === 'failed' ||
          status === 'failed' ||
          paymentStatus.status === 'cancelled' ||
          status === 'cancelled'
        ) {
          this.paymentStatusMessageSignal.set('Payment failed. Please try again.');
        } else {
          this.paymentStatusMessageSignal.set('Payment is still processing. We will email you once it completes.');
        }

        this.orderService.getOrderDetail(tenant, orderId).subscribe({
          next: detail => this.orderConfirmationSignal.set(detail),
          error: () => this.paymentStatusMessageSignal.set('Unable to load the order details. Please check later.')
        });
      },
      error: () => {
        if (status === 'success') {
          this.paymentStatusMessageSignal.set(
            'Payment captured. Your order will appear in the order history once the webhook confirms it.'
          );
        } else {
          this.paymentStatusMessageSignal.set('We could not verify your payment status. Please contact support.');
        }
      }
    });
  }

  private buildReturnUrl(tenant: string): string {
    if (typeof window === 'undefined') {
      return '';
    }

    const url = new URL(window.location.href);
    url.searchParams.set('paymentStatus', 'success');
    url.searchParams.delete('orderId');
    return url.toString();
  }

  private buildCancelUrl(tenant: string): string {
    if (typeof window === 'undefined') {
      return '';
    }

    const url = new URL(window.location.href);
    url.searchParams.set('paymentStatus', 'cancelled');
    url.searchParams.delete('orderId');
    return url.toString();
  }
}
