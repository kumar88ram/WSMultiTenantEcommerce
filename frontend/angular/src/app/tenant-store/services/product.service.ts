import { inject, Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, of, tap } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface ProductSummary {
  id: string;
  slug: string;
  name: string;
  price: number;
  currency: string;
  thumbnailUrl?: string;
  shortDescription?: string;
}

export interface ProductMedia {
  url: string;
  alt?: string;
}

export interface ProductVariant {
  id: string;
  name: string;
  price: number;
  currency: string;
  available: boolean;
  attributes?: Record<string, string>;
}

export interface ProductDetail extends ProductSummary {
  descriptionHtml?: string;
  media: ProductMedia[];
  variants: ProductVariant[];
  defaultVariantId?: string;
  attributeGroups?: Array<{
    name: string;
    values: string[];
  }>;
}

export interface FacetCategory {
  id: string;
  name: string;
  count: number;
}

export interface FacetAttributeValue {
  value: string;
  count: number;
}

export interface FacetAttribute {
  name: string;
  values: FacetAttributeValue[];
}

export interface ProductFacets {
  categories: FacetCategory[];
  attributes: FacetAttribute[];
  priceRange?: {
    min: number;
    max: number;
  };
}

export interface ProductSearchFilters {
  category?: string | null;
  attributes?: Record<string, string[]>;
  minPrice?: number | null;
  maxPrice?: number | null;
  search?: string | null;
  sort?: string | null;
  page: number;
  pageSize: number;
}

export interface ProductSearchResult {
  items: ProductSummary[];
  total: number;
  facets: ProductFacets;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/store`;

  private readonly searchCache = new Map<string, ProductSearchResult>();
  private readonly detailCache = new Map<string, ProductDetail>();

  searchProducts(tenant: string, filters: ProductSearchFilters): Observable<ProductSearchResult> {
    const cacheKey = this.buildSearchCacheKey(tenant, filters);
    const cached = this.searchCache.get(cacheKey);
    if (cached) {
      return of(cached);
    }

    const params = this.buildHttpParams(filters);
    return this.http
      .get<ProductSearchResult>(`${this.baseUrl}/${tenant}/products`, { params })
      .pipe(
        tap(result => {
          this.searchCache.set(cacheKey, result);
          result.items.forEach(item => {
            const detailKey = this.buildDetailCacheKey(tenant, item.slug);
            if (!this.detailCache.has(detailKey)) {
              this.detailCache.set(detailKey, {
                ...item,
                media: [],
                variants: []
              });
            }
          });
        })
      );
  }

  getProduct(tenant: string, productSlug: string): Observable<ProductDetail> {
    const cacheKey = this.buildDetailCacheKey(tenant, productSlug);
    const cached = this.detailCache.get(cacheKey);
    if (cached && cached.media.length && cached.variants.length) {
      return of(cached);
    }

    return this.http.get<ProductDetail>(`${this.baseUrl}/${tenant}/products/${productSlug}`).pipe(
      tap(detail => this.detailCache.set(cacheKey, detail))
    );
  }

  clearTenantCache(tenant?: string): void {
    if (!tenant) {
      this.searchCache.clear();
      this.detailCache.clear();
      return;
    }

    const tenantPrefix = `${tenant}|`;
    [...this.searchCache.keys()].forEach(key => {
      if (key.startsWith(tenantPrefix)) {
        this.searchCache.delete(key);
      }
    });

    [...this.detailCache.keys()].forEach(key => {
      if (key.startsWith(tenantPrefix)) {
        this.detailCache.delete(key);
      }
    });
  }

  private buildHttpParams(filters: ProductSearchFilters): HttpParams {
    let params = new HttpParams()
      .set('page', filters.page.toString())
      .set('pageSize', filters.pageSize.toString());

    if (filters.category) {
      params = params.set('category', filters.category);
    }

    if (filters.minPrice != null) {
      params = params.set('minPrice', filters.minPrice.toString());
    }

    if (filters.maxPrice != null) {
      params = params.set('maxPrice', filters.maxPrice.toString());
    }

    if (filters.search) {
      params = params.set('search', filters.search);
    }

    if (filters.sort) {
      params = params.set('sort', filters.sort);
    }

    if (filters.attributes) {
      const attributeEntries = Object.entries(filters.attributes).sort(([a], [b]) => a.localeCompare(b));
      attributeEntries.forEach(([name, values]) => {
        values
          .slice()
          .sort()
          .forEach(value => {
            params = params.append(`attr_${name}`, value);
          });
      });
    }

    return params;
  }

  private buildSearchCacheKey(tenant: string, filters: ProductSearchFilters): string {
    const normalized: ProductSearchFilters = {
      ...filters,
      attributes: filters.attributes
        ? Object.keys(filters.attributes)
            .sort()
            .reduce<Record<string, string[]>>((acc, key) => {
              const values = filters.attributes![key];
              acc[key] = [...values].sort();
              return acc;
            }, {})
        : undefined
    };

    return `${tenant}|${JSON.stringify(normalized)}`;
  }

  private buildDetailCacheKey(tenant: string, slug: string): string {
    return `${tenant}|${slug}`;
  }
}
