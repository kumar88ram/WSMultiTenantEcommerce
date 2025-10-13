import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Subscription, filter, switchMap } from 'rxjs';
import {
  TenantAdminProductService,
  TenantProductDraft
} from '../../services/tenant-admin-product.service';

@Component({
  selector: 'app-product-edit',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatSlideToggleModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressBarModule,
    MatTooltipModule
  ],
  template: `
    <section class="product-edit" *ngIf="form">
      <header>
        <div>
          <h1>{{ productId() ? 'Edit product' : 'Create product' }}</h1>
          <p class="subtitle">Compose detailed product information, upload imagery and publish changes.</p>
        </div>
        <div class="actions">
          <button mat-stroked-button color="primary" (click)="save()" [disabled]="form.invalid || saving()">
            <mat-icon>save</mat-icon>
            Save product
          </button>
        </div>
      </header>

      <div class="layout">
        <mat-card class="main">
          <mat-card-header>
            <mat-card-title>Product content</mat-card-title>
            <mat-card-subtitle>Use the rich text editor to describe your product in depth.</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <form [formGroup]="form" class="form-grid">
              <mat-form-field appearance="outline">
                <mat-label>Name</mat-label>
                <input matInput formControlName="name" placeholder="Premium cotton t-shirt" required />
                <mat-error *ngIf="form.controls.name.hasError('required')">Name is required</mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>SKU</mat-label>
                <input matInput formControlName="sku" placeholder="SKU-001" required />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Price</mat-label>
                <input matInput type="number" min="0" step="0.01" formControlName="price" required />
              </mat-form-field>

              <mat-form-field appearance="outline">
                <mat-label>Currency</mat-label>
                <mat-select formControlName="currency" required>
                  <mat-option value="USD">USD</mat-option>
                  <mat-option value="EUR">EUR</mat-option>
                  <mat-option value="GBP">GBP</mat-option>
                </mat-select>
              </mat-form-field>

              <mat-slide-toggle formControlName="published">Published</mat-slide-toggle>
            </form>

            <section class="editor">
              <header>
                <h2>Description</h2>
                <div class="toolbar">
                  <button mat-icon-button type="button" (click)="applyFormat('bold')" matTooltip="Bold">
                    <mat-icon>format_bold</mat-icon>
                  </button>
                  <button mat-icon-button type="button" (click)="applyFormat('italic')" matTooltip="Italic">
                    <mat-icon>format_italic</mat-icon>
                  </button>
                  <button mat-icon-button type="button" (click)="applyFormat('insertUnorderedList')" matTooltip="Bullet list">
                    <mat-icon>format_list_bulleted</mat-icon>
                  </button>
                </div>
              </header>
              <div
                class="editor-surface"
                contenteditable
                [innerHTML]="descriptionHtml()"
                (input)="onEditorInput($event)"
              ></div>
            </section>
          </mat-card-content>
        </mat-card>

        <div class="side">
          <mat-card>
            <mat-card-header>
              <mat-card-title>Media</mat-card-title>
              <mat-card-subtitle>Upload a primary image shown on product listings.</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <div class="media-upload" [class.uploading]="uploading()">
                <img *ngIf="imageUrl()" [src]="imageUrl()" alt="Product" />
                <button mat-stroked-button color="primary" type="button" (click)="fileInput.click()">
                  <mat-icon>cloud_upload</mat-icon>
                  {{ imageUrl() ? 'Replace image' : 'Upload image' }}
                </button>
                <input #fileInput type="file" accept="image/*" hidden (change)="onFileSelected($event)" />
              </div>
              <mat-progress-bar *ngIf="uploading()" mode="indeterminate"></mat-progress-bar>
            </mat-card-content>
          </mat-card>

          <mat-card>
            <mat-card-header>
              <mat-card-title>Metadata</mat-card-title>
              <mat-card-subtitle>SEO summary for search engines.</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>SEO title</mat-label>
                <input matInput formControlName="seoTitle" />
              </mat-form-field>
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>SEO description</mat-label>
                <textarea matInput rows="4" formControlName="seoDescription"></textarea>
              </mat-form-field>
            </mat-card-content>
          </mat-card>
        </div>
      </div>
    </section>
  `,
  styles: [
    `
      .product-edit {
        display: flex;
        flex-direction: column;
        gap: 2rem;
      }

      header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        flex-wrap: wrap;
      }

      .subtitle {
        color: rgba(0, 0, 0, 0.6);
        margin: 0;
      }

      .layout {
        display: grid;
        grid-template-columns: 2fr 1fr;
        gap: 1.5rem;
        align-items: start;
      }

      mat-card.main mat-card-content {
        display: flex;
        flex-direction: column;
        gap: 2rem;
      }

      .form-grid {
        display: grid;
        gap: 1rem;
        grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
        align-items: center;
      }

      .editor {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
      }

      .editor header {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }

      .editor-surface {
        min-height: 240px;
        border: 1px solid rgba(0, 0, 0, 0.12);
        border-radius: 0.75rem;
        padding: 1rem;
        outline: none;
      }

      .editor-surface:focus {
        border-color: rgba(63, 81, 181, 0.8);
        box-shadow: 0 0 0 1px rgba(63, 81, 181, 0.2);
      }

      .toolbar button {
        margin-left: 0.25rem;
      }

      .media-upload {
        display: grid;
        gap: 1rem;
        justify-items: center;
      }

      .media-upload img {
        max-width: 100%;
        border-radius: 0.75rem;
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.08);
      }

      .media-upload.uploading {
        opacity: 0.6;
        pointer-events: none;
      }

      .side {
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
      }

      .full-width {
        width: 100%;
      }

      @media (max-width: 1280px) {
        .layout {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class ProductEditComponent implements OnInit, OnDestroy {
  private readonly fb = inject(FormBuilder);
  private readonly productService = inject(TenantAdminProductService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly snackBar = inject(MatSnackBar);

  private readonly subscription = new Subscription();

  protected readonly productId = signal<string | null>(null);
  protected readonly descriptionHtml = signal('');
  protected readonly imageUrl = signal<string | null>(null);
  protected readonly uploading = signal(false);
  protected readonly saving = signal(false);

  form = this.fb.nonNullable.group({
    name: ['', Validators.required],
    sku: ['', Validators.required],
    price: [0, [Validators.required, Validators.min(0)]],
    currency: ['USD', Validators.required],
    published: [false],
    seoTitle: [''],
    seoDescription: ['']
  });

  ngOnInit(): void {
    const paramsSub = this.route.paramMap
      .pipe(
        filter(params => params.has('productId')),
        switchMap(params => this.productService.getProduct(params.get('productId')!))
      )
      .subscribe(product => {
        this.productId.set(product.id);
        this.form.patchValue({
          name: product.name,
          sku: product.sku,
          price: product.price,
          currency: product.currency,
          published: product.published,
          seoTitle: product.seoTitle ?? '',
          seoDescription: product.seoDescription ?? ''
        });
        this.descriptionHtml.set(product.descriptionHtml ?? '');
        this.imageUrl.set(product.imageUrl ?? null);
      });

    this.subscription.add(paramsSub);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  onEditorInput(event: Event): void {
    const target = event.target as HTMLElement;
    this.descriptionHtml.set(target.innerHTML);
  }

  applyFormat(command: string): void {
    document.execCommand(command);
  }

  onFileSelected(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) {
      return;
    }

    this.uploading.set(true);
    const uploadSub = this.productService.uploadProductImage(file).subscribe({
      next: response => {
        this.imageUrl.set(response.url);
        this.uploading.set(false);
        this.snackBar.open('Image uploaded', 'Close', { duration: 2000 });
      },
      error: () => {
        this.uploading.set(false);
        this.snackBar.open('Failed to upload image', 'Close', { duration: 3000 });
      }
    });

    this.subscription.add(uploadSub);
  }

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.saving.set(true);

    const draft: TenantProductDraft = {
      ...this.form.getRawValue(),
      descriptionHtml: this.descriptionHtml(),
      imageUrl: this.imageUrl() ?? undefined,
      id: this.productId() ?? undefined
    };

    const saveSub = this.productService.saveProduct(draft).subscribe({
      next: product => {
        this.saving.set(false);
        this.snackBar.open('Product saved', 'Close', { duration: 2000 });
        if (!this.productId()) {
          this.router.navigate(['/tenant-admin/products', product.id]);
        }
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Failed to save product', 'Close', { duration: 3000 });
      }
    });

    this.subscription.add(saveSub);
  }
}
