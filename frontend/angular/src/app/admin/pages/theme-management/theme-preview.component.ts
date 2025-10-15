import { CommonModule } from '@angular/common';
import { Component, Input } from '@angular/core';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { ThemeSummary } from '../../../core/models/theme.models';

@Component({
  selector: 'app-theme-preview',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatChipsModule],
  template: `
    <mat-card *ngIf="theme; else empty">
      <mat-card-header>
        <mat-card-title>{{ theme?.name }}</mat-card-title>
        <mat-card-subtitle>{{ theme?.code }} · v{{ theme?.version }}</mat-card-subtitle>
      </mat-card-header>
      <img mat-card-image *ngIf="theme?.previewImageUrl" [src]="theme?.previewImageUrl" alt="Theme preview" />
      <mat-card-content>
        <p>{{ theme?.description || 'No description provided.' }}</p>
        <div class="sections" *ngIf="theme?.sections?.length">
          <span>Sections:</span>
          <mat-chip-listbox>
            <mat-chip *ngFor="let section of theme!.sections">{{ section.sectionName }}</mat-chip>
          </mat-chip-listbox>
        </div>
      </mat-card-content>
    </mat-card>
    <ng-template #empty>
      <mat-card>
        <mat-card-content>
          <p>Select a theme to preview its details.</p>
        </mat-card-content>
      </mat-card>
    </ng-template>
  `,
  styles: [
    `
      mat-card {
        width: 100%;
      }
      .sections {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        margin-top: 1rem;
      }
    `
  ]
})
export class ThemePreviewComponent {
  @Input() theme: ThemeSummary | null = null;
}
