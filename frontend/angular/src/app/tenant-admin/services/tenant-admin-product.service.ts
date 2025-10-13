import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface TenantProductSummary {
  id: string;
  name: string;
  sku: string;
  price: number;
  currency: string;
  published: boolean;
  updatedAt: string;
}

export interface TenantProductDetail extends TenantProductSummary {
  descriptionHtml?: string;
  imageUrl?: string;
  seoTitle?: string;
  seoDescription?: string;
}

export interface TenantProductDraft {
  id?: string;
  name: string;
  sku: string;
  price: number;
  currency: string;
  published: boolean;
  descriptionHtml?: string;
  imageUrl?: string;
  seoTitle?: string;
  seoDescription?: string;
}

export interface UploadImageResponse {
  url: string;
}

@Injectable({ providedIn: 'root' })
export class TenantAdminProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/tenant/products`;

  getProducts(): Observable<TenantProductSummary[]> {
    return this.http.get<TenantProductSummary[]>(this.baseUrl);
  }

  getProduct(id: string): Observable<TenantProductDetail> {
    return this.http.get<TenantProductDetail>(`${this.baseUrl}/${id}`);
  }

  saveProduct(draft: TenantProductDraft): Observable<TenantProductDetail> {
    if (draft.id) {
      return this.http.put<TenantProductDetail>(`${this.baseUrl}/${draft.id}`, draft);
    }

    return this.http.post<TenantProductDetail>(this.baseUrl, draft);
  }

  uploadProductImage(file: File): Observable<UploadImageResponse> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<UploadImageResponse>(`${this.baseUrl}/media`, formData);
  }
}
