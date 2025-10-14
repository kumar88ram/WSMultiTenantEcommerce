import {
  AfterViewInit,
  Component,
  DestroyRef,
  ElementRef,
  Signal,
  ViewChild,
  effect,
  inject,
  signal
} from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule, ActivatedRoute } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { map } from 'rxjs/operators';
import { Subscription } from 'rxjs';
import { ProductService, ProductSummary, ProductFacets } from '../../services/product.service';
import { TenantStoreService } from '../../services/tenant-store.service';

interface FilterState {
  category: string | null;
  attributes: Record<string, string[]>;
  minPrice: number | null;
  maxPrice: number | null;
  search: string | null;
}

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  template: `
    <div class="storefront" *ngIf="tenant(); else missingTenant">
      <aside class="filters">
        <h2>Filters</h2>

        <section *ngIf="facets()?.categories?.length" class="filter-section">
          <h3>Categories</h3>
          <ul>
            <li>
              <label>
                <input
                  type="radio"
                  name="category"
                  [checked]="!filters().category"
                  (change)="updateCategory(null)"
                />
                All
              </label>
            </li>
            <li *ngFor="let category of facets()?.categories">
              <label>
                <input
                  type="radio"
                  name="category"
                  [value]="category.id"
                  [checked]="filters().category === category.id"
                  (change)="updateCategory(category.id)"
                />
                {{ category.name }} <span class="count">({{ category.count }})</span>
              </label>
            </li>
          </ul>
        </section>

        <section *ngIf="facets()?.attributes?.length" class="filter-section">
          <h3>Attributes</h3>
          <div *ngFor="let attribute of facets()?.attributes" class="attribute-group">
            <p>{{ attribute.name }}</p>
            <ul>
              <li *ngFor="let value of attribute.values">
                <label>
                  <input
                    type="checkbox"
                    [checked]="isAttributeSelected(attribute.name, value.value)"
                    (change)="toggleAttribute(attribute.name, value.value)"
                  />
                  {{ value.value }} <span class="count">({{ value.count }})</span>
                </label>
              </li>
            </ul>
          </div>
        </section>

        <section class="filter-section">
          <h3>Price Range</h3>
          <div class="price-range">
            <label>
              Min
              <input type="number" [(ngModel)]="priceMinInput" (change)="applyPriceRange()" />
            </label>
            <label>
              Max
              <input type="number" [(ngModel)]="priceMaxInput" (change)="applyPriceRange()" />
            </label>
            <button type="button" (click)="clearPriceRange()" [disabled]="!filters().minPrice && !filters().maxPrice">
              Clear
            </button>
          </div>
        </section>

        <section class="filter-section">
          <h3>Search</h3>
          <input
            type="search"
            placeholder="Search products"
            [(ngModel)]="searchTerm"
            (ngModelChange)="applySearch()"
          />
        </section>
      </aside>

      <section class="product-results">
        <header class="results-header">
          <h2>Products</h2>
          <p *ngIf="totalCount() !== null">Showing {{ products().length }} of {{ totalCount() }} results</p>
        </header>

        <div class="product-grid" *ngIf="products().length; else emptyState">
          <article *ngFor="let product of products()" class="product-card">
            <a [routerLink]="[product.slug]" class="product-link">
              <img [src]="product.thumbnailUrl || placeholderImage" [alt]="product.name" />
              <div class="product-info">
                <h3>{{ product.name }}</h3>
                <p class="description">{{ product.shortDescription || 'Discover more details' }}</p>
                <p class="price">{{ product.price | currency: product.currency }}</p>
              </div>
            </a>
          </article>
        </div>

        <div class="loading" *ngIf="loading()">Loading products…</div>
        <div class="error" *ngIf="error()">{{ error() }}</div>
        <div #infiniteAnchor class="infinite-anchor" aria-hidden="true"></div>
      </section>
    </div>

    <ng-template #emptyState>
      <p class="empty">No products match the selected filters.</p>
    </ng-template>

    <ng-template #missingTenant>
      <p class="error">Tenant not specified.</p>
    </ng-template>
  `,
  styles: [
    `
      .storefront {
        display: grid;
        grid-template-columns: minmax(220px, 280px) 1fr;
        gap: 2rem;
        padding: 1.5rem;
      }

      .filters {
        border-right: 1px solid #e0e0e0;
        padding-right: 1.5rem;
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
      }

      .filter-section h3 {
        margin-bottom: 0.5rem;
      }

      .filter-section ul {
        list-style: none;
        padding: 0;
        margin: 0;
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
      }

      .filter-section label {
        display: flex;
        align-items: center;
        gap: 0.5rem;
        cursor: pointer;
      }

      .attribute-group + .attribute-group {
        margin-top: 1rem;
      }

      .price-range {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      .price-range label {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
      }

      .price-range input {
        padding: 0.25rem 0.5rem;
      }

      .product-results {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .results-header {
        display: flex;
        align-items: baseline;
        justify-content: space-between;
      }

      .product-grid {
        display: grid;
        gap: 1.5rem;
        grid-template-columns: repeat(auto-fill, minmax(220px, 1fr));
      }

      .product-card {
        border: 1px solid #e0e0e0;
        border-radius: 8px;
        overflow: hidden;
        background: #fff;
        transition: box-shadow 0.2s ease;
      }

      .product-card:hover {
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
      }

      .product-card img {
        width: 100%;
        aspect-ratio: 4 / 3;
        object-fit: cover;
      }

      .product-info {
        padding: 1rem;
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
      }

      .price {
        font-weight: 600;
      }

      .count {
        color: #757575;
      }

      .error {
        color: #b00020;
      }

      .empty {
        padding: 2rem;
        text-align: center;
      }

      .loading {
        text-align: center;
        padding: 1rem;
      }

      .infinite-anchor {
        height: 1px;
      }

      @media (max-width: 960px) {
        .storefront {
          grid-template-columns: 1fr;
        }

        .filters {
          border-right: none;
          border-bottom: 1px solid #e0e0e0;
          padding-bottom: 1.5rem;
        }
      }
    `
  ]
})
export class ProductListComponent implements AfterViewInit {
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);
  private readonly tenantStore = inject(TenantStoreService);
  private readonly destroyRef = inject(DestroyRef);

  @ViewChild('infiniteAnchor') private infiniteAnchor?: ElementRef<HTMLDivElement>;

  private intersectionObserver?: IntersectionObserver;
  private searchSubscription?: Subscription;
  private lastTenant: string | null = null;

  readonly tenant: Signal<string> = toSignal(
    this.route.paramMap.pipe(map(params => params.get('tenant') ?? '')),
    { initialValue: '' }
  );

  readonly filters = signal<FilterState>({
    category: null,
    attributes: {},
    minPrice: null,
    maxPrice: null,
    search: null
  });

  readonly products = signal<ProductSummary[]>([]);
  readonly facets = signal<ProductFacets | null>(null);
  readonly totalCount = signal<number | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly page = signal(0);
  readonly hasMore = signal(true);

  priceMinInput: number | null = null;
  priceMaxInput: number | null = null;
  searchTerm = '';

  readonly placeholderImage = 'https://via.placeholder.com/400x300?text=Product';

  constructor() {
    this.destroyRef.onDestroy(() => {
      this.intersectionObserver?.disconnect();
      this.searchSubscription?.unsubscribe();
    });

    effect(
      () => {
        const tenant = this.tenant();
        if (!tenant) {
          return;
        }

        this.tenantStore.setTenant(tenant);

        if (tenant !== this.lastTenant) {
          this.lastTenant = tenant;
          this.filters.set({
            category: null,
            attributes: {},
            minPrice: null,
            maxPrice: null,
            search: null
          });
          this.priceMinInput = null;
          this.priceMaxInput = null;
          this.searchTerm = '';
          return;
        }

        const currentFilters = this.filters();
        this.resetPagination();
        this.loadPage(tenant, 1, currentFilters);
      },
      { allowSignalWrites: true }
    );
  }

  ngAfterViewInit(): void {
    this.setupIntersectionObserver();
  }

  private setupIntersectionObserver(): void {
    this.intersectionObserver?.disconnect();
    if (!this.infiniteAnchor) {
      return;
    }

    this.intersectionObserver = new IntersectionObserver(entries => {
      if (entries.some(entry => entry.isIntersecting)) {
        this.loadNextPage();
      }
    }, { rootMargin: '200px 0px' });

    this.intersectionObserver.observe(this.infiniteAnchor.nativeElement);

    this.destroyRef.onDestroy(() => this.intersectionObserver?.disconnect());
  }

  private resetPagination(): void {
    this.page.set(0);
    this.products.set([]);
    this.hasMore.set(true);
    this.facets.set(null);
    this.totalCount.set(null);
    this.error.set(null);
  }

  private loadPage(tenant: string, page: number, filters: FilterState): void {
    if (this.loading()) {
      this.searchSubscription?.unsubscribe();
    }

    this.error.set(null);
    this.loading.set(true);

    const searchFilters = {
      category: filters.category,
      attributes: filters.attributes,
      minPrice: filters.minPrice,
      maxPrice: filters.maxPrice,
      search: filters.search,
      page,
      pageSize: 20,
      sort: null
    } as const;

    this.searchSubscription?.unsubscribe();
    this.searchSubscription = this.productService
      .searchProducts(tenant, searchFilters)
      .subscribe({
        next: result => {
          const nextProducts = [...this.products(), ...result.items];
          this.products.set(nextProducts);
          this.page.set(page);
          this.totalCount.set(result.total);
          this.facets.set(result.facets);
          this.hasMore.set(nextProducts.length < result.total);
          this.loading.set(false);
          this.searchSubscription = undefined;
        },
        error: () => {
          this.error.set('Unable to load products. Please try again.');
          this.loading.set(false);
          this.searchSubscription = undefined;
        }
      });

  }

  private loadNextPage(): void {
    if (!this.hasMore() || this.loading()) {
      return;
    }

    const tenant = this.tenant();
    if (!tenant) {
      return;
    }

    this.loadPage(tenant, this.page() + 1, this.filters());
  }

  updateCategory(category: string | null): void {
    this.filters.update(state => ({ ...state, category }));
  }

  toggleAttribute(name: string, value: string): void {
    this.filters.update(state => {
      const currentValues = new Set(state.attributes[name] ?? []);
      if (currentValues.has(value)) {
        currentValues.delete(value);
      } else {
        currentValues.add(value);
      }

      const attributes = { ...state.attributes };
      if (currentValues.size) {
        attributes[name] = Array.from(currentValues);
      } else {
        delete attributes[name];
      }

      return {
        ...state,
        attributes
      };
    });
  }

  isAttributeSelected(name: string, value: string): boolean {
    return this.filters().attributes[name]?.includes(value) ?? false;
  }

  applyPriceRange(): void {
    const minRaw = this.priceMinInput;
    const maxRaw = this.priceMaxInput;
    const minPrice = minRaw != null && !Number.isNaN(Number(minRaw)) ? Number(minRaw) : null;
    const maxPrice = maxRaw != null && !Number.isNaN(Number(maxRaw)) ? Number(maxRaw) : null;
    this.priceMinInput = minPrice;
    this.priceMaxInput = maxPrice;
    this.filters.update(state => ({
      ...state,
      minPrice,
      maxPrice
    }));
  }

  clearPriceRange(): void {
    this.priceMinInput = null;
    this.priceMaxInput = null;
    this.filters.update(state => ({
      ...state,
      minPrice: null,
      maxPrice: null
    }));
  }

  applySearch(): void {
    const term = this.searchTerm.trim();
    this.searchTerm = term;
    this.filters.update(state => ({
      ...state,
      search: term ? term : null
    }));
  }
}
