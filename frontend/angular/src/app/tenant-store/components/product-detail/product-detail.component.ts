import { Component, DestroyRef, Signal, computed, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { ProductDetail, ProductService, ProductVariant } from '../../services/product.service';
import { TenantStoreService } from '../../services/tenant-store.service';
import { CartService } from '../../services/cart.service';

@Component({
  selector: 'app-product-detail',
  standalone: true,
  imports: [CommonModule, RouterModule],
  template: `
    <ng-container *ngIf="!loading(); else loadingState">
      <div class="product-detail" *ngIf="product(); else errorState">
        <a routerLink="../" class="back-link">← Back to products</a>

        <div class="product-layout">
          <section class="media">
            <div class="main-image">
              <img
                [src]="selectedImage()?.url || placeholderImage"
                [alt]="selectedImage()?.alt || product()?.name || 'Product image'"
              />
            </div>
            <div class="thumbnails" *ngIf="product()?.media?.length">
              <button
                type="button"
                *ngFor="let image of product()?.media; index as index"
                [class.active]="index === selectedImageIndex()"
                (click)="selectImage(index)"
              >
                <img [src]="image.url" [alt]="image.alt || product()?.name || 'Product image'" />
              </button>
            </div>
          </section>

          <section class="details">
            <h1>{{ product()?.name }}</h1>
            <p class="price">{{ displayPrice() | currency: displayCurrency() }}</p>

            <div class="variant-selector" *ngIf="product()?.variants?.length">
              <h3>Select a variant</h3>
              <div class="variant-grid">
                <button
                  type="button"
                  *ngFor="let variant of product()?.variants"
                  [disabled]="!variant.available"
                  [class.active]="variant.id === selectedVariantId()"
                  (click)="selectVariant(variant)"
                >
                  <span class="variant-name">{{ variant.name }}</span>
                  <span class="variant-price">{{ variant.price | currency: variant.currency }}</span>
                  <span class="availability" *ngIf="!variant.available">Out of stock</span>
                </button>
              </div>
            </div>

            <button
              class="add-to-cart"
              type="button"
              (click)="addToCart()"
              [disabled]="product()?.variants?.length ? !selectedVariant() : false"
            >
              Add to cart
            </button>
            <p class="add-to-cart-feedback" *ngIf="addToCartFeedback()">{{ addToCartFeedback() }}</p>

            <section class="attributes" *ngIf="product()?.attributeGroups?.length">
              <h3>Product details</h3>
              <dl>
                <ng-container *ngFor="let attribute of product()?.attributeGroups">
                  <dt>{{ attribute.name }}</dt>
                  <dd>{{ attribute.values.join(', ') }}</dd>
                </ng-container>
              </dl>
            </section>

            <section class="description" *ngIf="product()?.descriptionHtml">
              <h3>Description</h3>
              <div [innerHTML]="product()?.descriptionHtml"></div>
            </section>
          </section>
        </div>
      </div>
    </ng-container>

    <ng-template #loadingState>
      <div class="loading">Loading product…</div>
    </ng-template>

    <ng-template #errorState>
      <div class="error">{{ error() || 'Product not found.' }}</div>
    </ng-template>
  `,
  styles: [
    `
      .product-detail {
        padding: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
      }

      .back-link {
        color: #1976d2;
        text-decoration: none;
      }

      .product-layout {
        display: grid;
        gap: 2rem;
        grid-template-columns: repeat(auto-fit, minmax(320px, 1fr));
        align-items: start;
      }

      .media {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .main-image img {
        width: 100%;
        border-radius: 12px;
        object-fit: cover;
        box-shadow: 0 8px 24px rgba(0, 0, 0, 0.12);
      }

      .thumbnails {
        display: flex;
        gap: 0.75rem;
        flex-wrap: wrap;
      }

      .thumbnails button {
        border: none;
        padding: 0;
        border-radius: 8px;
        overflow: hidden;
        cursor: pointer;
        opacity: 0.7;
        border: 2px solid transparent;
      }

      .thumbnails button.active {
        opacity: 1;
        border-color: #1976d2;
      }

      .thumbnails img {
        width: 72px;
        height: 72px;
        object-fit: cover;
        display: block;
      }

      .details {
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
      }

      .price {
        font-size: 1.5rem;
        font-weight: 600;
      }

      .variant-grid {
        display: grid;
        gap: 0.75rem;
        grid-template-columns: repeat(auto-fill, minmax(160px, 1fr));
      }

      .variant-grid button {
        padding: 0.75rem;
        border: 1px solid #cfd8dc;
        border-radius: 8px;
        background: #fff;
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        align-items: flex-start;
        cursor: pointer;
        transition: border-color 0.2s ease, box-shadow 0.2s ease;
      }

      .variant-grid button:hover {
        border-color: #1976d2;
        box-shadow: 0 4px 16px rgba(25, 118, 210, 0.1);
      }

      .variant-grid button.active {
        border-color: #1976d2;
        box-shadow: 0 4px 16px rgba(25, 118, 210, 0.15);
      }

      .variant-grid button:disabled {
        cursor: not-allowed;
        opacity: 0.6;
      }

      .add-to-cart {
        padding: 0.75rem 1.25rem;
        border: none;
        border-radius: 999px;
        background: #1976d2;
        color: #fff;
        font-size: 1rem;
        font-weight: 600;
        cursor: pointer;
        align-self: flex-start;
      }

      .add-to-cart:disabled {
        opacity: 0.6;
        cursor: not-allowed;
      }

      .add-to-cart-feedback {
        color: #388e3c;
      }

      .attributes dl {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(160px, 1fr));
        gap: 0.5rem 1.5rem;
      }

      .attributes dt {
        font-weight: 600;
      }

      .attributes dd {
        margin: 0;
      }

      .description {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
      }

      .loading,
      .error {
        padding: 2rem;
        text-align: center;
      }

      .error {
        color: #b00020;
      }

      @media (max-width: 720px) {
        .product-detail {
          padding: 1rem;
        }

        .variant-grid {
          grid-template-columns: repeat(auto-fill, minmax(140px, 1fr));
        }
      }
    `
  ]
})
export class ProductDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);
  private readonly tenantStore = inject(TenantStoreService);
  private readonly cartService = inject(CartService);
  private readonly destroyRef = inject(DestroyRef);
  private productSubscription?: Subscription;

  readonly tenant: Signal<string> = toSignal(
    this.route.paramMap.pipe(map(params => params.get('tenant') ?? '')),
    { initialValue: '' }
  );

  readonly productSlug: Signal<string> = toSignal(
    this.route.paramMap.pipe(map(params => params.get('productSlug') ?? '')),
    { initialValue: '' }
  );

  readonly product = signal<ProductDetail | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly selectedImageIndex = signal(0);
  readonly selectedVariantId = signal<string | null>(null);
  readonly addToCartFeedback = signal<string | null>(null);

  readonly placeholderImage = 'https://via.placeholder.com/800x600?text=Product';

  readonly selectedVariant: Signal<ProductVariant | null> = computed(() => {
    const detail = this.product();
    const variantId = this.selectedVariantId();
    if (!detail || !variantId) {
      return null;
    }

    return detail.variants.find(variant => variant.id === variantId) ?? null;
  });

  readonly selectedImage = computed(() => {
    const detail = this.product();
    const index = this.selectedImageIndex();
    if (!detail?.media?.length) {
      return null;
    }

    return detail.media[Math.min(Math.max(index, 0), detail.media.length - 1)];
  });

  readonly displayPrice = computed(() => {
    const variant = this.selectedVariant();
    if (variant) {
      return variant.price;
    }

    return this.product()?.price ?? 0;
  });

  readonly displayCurrency = computed(() => {
    const variant = this.selectedVariant();
    if (variant) {
      return variant.currency;
    }

    return this.product()?.currency ?? 'USD';
  });

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.addToCartFeedback.set(null);
      this.productSubscription?.unsubscribe();
    });

    effect(
      () => {
        const tenant = this.tenant();
        const slug = this.productSlug();
        if (!tenant || !slug) {
          return;
        }

        this.tenantStore.setTenant(tenant);
        this.fetchProduct(tenant, slug);
      },
      { allowSignalWrites: true }
    );
  }

  private fetchProduct(tenant: string, slug: string): void {
    this.loading.set(true);
    this.error.set(null);
    this.productSubscription?.unsubscribe();

    this.productSubscription = this.productService
      .getProduct(tenant, slug)
      .subscribe({
        next: detail => {
          this.product.set(detail);
          this.loading.set(false);
          this.selectedImageIndex.set(0);
          const variants = detail.variants ?? [];
          const defaultVariantId = detail.defaultVariantId || variants.find(v => v.available)?.id || null;
          this.selectedVariantId.set(defaultVariantId);
          this.addToCartFeedback.set(null);
          this.productSubscription = undefined;
        },
        error: () => {
          this.error.set('Unable to load the product. Please try again later.');
          this.loading.set(false);
          this.product.set(null);
          this.selectedVariantId.set(null);
          this.productSubscription = undefined;
        }
      });
  }

  selectVariant(variant: ProductVariant): void {
    if (!variant.available) {
      return;
    }

    this.selectedVariantId.set(variant.id);
    this.addToCartFeedback.set(null);
  }

  selectImage(index: number): void {
    this.selectedImageIndex.set(index);
  }

  addToCart(): void {
    const detail = this.product();
    if (!detail) {
      return;
    }

    const variant = this.selectedVariant();
    if (detail.variants?.length && !variant) {
      this.addToCartFeedback.set('Please choose a variant before adding to cart.');
      return;
    }

    const label = variant ? `${detail.name} (${variant.name})` : detail.name;
    const tenant = this.tenant();
    if (!tenant) {
      this.addToCartFeedback.set('Unable to determine the tenant store.');
      return;
    }

    this.cartService
      .addItem(tenant, {
        productId: detail.id,
        variantId: variant?.id ?? null,
        quantity: 1
      })
      .subscribe({
        next: () => this.addToCartFeedback.set(`Added "${label}" to cart.`),
        error: () => this.addToCartFeedback.set('Unable to add the item to your cart. Please try again.')
      });
  }
}
