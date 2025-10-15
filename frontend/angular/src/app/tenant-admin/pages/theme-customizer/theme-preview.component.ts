import { CommonModule } from '@angular/common';
import { Component, Input, computed, inject } from '@angular/core';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-tenant-theme-preview',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="preview" *ngIf="previewUrl; else placeholder">
      <iframe [src]="safeUrl()" title="Theme preview" class="preview-frame"></iframe>
    </div>
    <ng-template #placeholder>
      <div class="preview placeholder">
        <p>No live preview available. Upload a logo or adjust colors to see the effect.</p>
      </div>
    </ng-template>
  `,
  styles: [
    `
      .preview {
        border: 1px solid rgba(0, 0, 0, 0.12);
        border-radius: 8px;
        overflow: hidden;
        height: 420px;
      }
      .preview-frame {
        width: 100%;
        height: 100%;
        border: none;
      }
      .placeholder {
        display: flex;
        align-items: center;
        justify-content: center;
        padding: 2rem;
        color: rgba(0, 0, 0, 0.6);
      }
    `
  ]
})
export class ThemePreviewComponent {
  @Input() previewUrl: string | null = null;

  private readonly sanitizer = inject(DomSanitizer);

  protected readonly safeUrl = computed<SafeResourceUrl | null>(() =>
    this.previewUrl ? this.sanitizer.bypassSecurityTrustResourceUrl(this.previewUrl) : null
  );
}
