import { CommonModule } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterModule } from '@angular/router';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatListModule } from '@angular/material/list';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatToolbarModule } from '@angular/material/toolbar';
import { ThemeService } from '../../../core/services/theme.service';
import { ThemeSummary, TenantTheme } from '../../../core/models/theme.models';
import { ThemeUploadComponent } from './theme-upload.component';
import { ThemePreviewComponent } from './theme-preview.component';
import { ThemeActivationComponent } from './theme-activation.component';
import { ThemeSectionBuilderComponent } from './theme-section-builder.component';
import { ThemeCloneDialogComponent } from './theme-clone-dialog.component';

@Component({
  selector: 'app-theme-list',
  standalone: true,
  imports: [
    CommonModule,
    MatButtonModule,
    MatDividerModule,
    MatListModule,
    MatProgressBarModule,
    MatToolbarModule,
    MatDialogModule,
    MatSnackBarModule,
    RouterModule,
    ThemeUploadComponent,
    ThemePreviewComponent,
    ThemeActivationComponent,
    ThemeSectionBuilderComponent,
    ThemeCloneDialogComponent
  ],
  template: `
    <section class="theme-manager">
      <mat-toolbar color="primary" class="toolbar">
        <span>Store Themes</span>
        <span class="spacer"></span>
        <button mat-button routerLink="analytics">Usage Analytics</button>
        <button mat-stroked-button (click)="openCloneDialog()">Clone Theme</button>
        <app-theme-upload (themeUploaded)="handleThemeUploaded($event)"></app-theme-upload>
      </mat-toolbar>

      <div class="content">
        <aside class="sidebar">
          <h3>Available Themes</h3>
          <mat-progress-bar *ngIf="loading()" mode="indeterminate"></mat-progress-bar>
          <mat-nav-list>
            <a
              mat-list-item
              *ngFor="let theme of themes()"
              [class.active]="theme.id === selectedTheme()?.id"
              (click)="selectTheme(theme)"
            >
              <span matListItemTitle>{{ theme.name }}</span>
              <span matListItemLine>{{ theme.code }} · v{{ theme.version }}</span>
            </a>
          </mat-nav-list>
        </aside>

        <section class="details" *ngIf="selectedTheme(); else emptyState">
          <app-theme-preview [theme]="selectedTheme()"></app-theme-preview>
          <mat-divider></mat-divider>
          <app-theme-activation
            [theme]="selectedTheme()"
            (themeActivated)="handleActivation($event)"
            (themeDeactivated)="refreshThemes()"
          ></app-theme-activation>
          <mat-divider></mat-divider>
          <app-theme-section-builder [theme]="selectedTheme()" (sectionsSaved)="refreshThemes()"></app-theme-section-builder>
        </section>
        <ng-template #emptyState>
          <section class="empty">
            <h3>Select a theme to configure</h3>
            <p>Upload a new theme or pick one from the list to manage activation, sections, and preview.</p>
          </section>
        </ng-template>
      </div>
    </section>
  `,
  styles: [
    `
      .theme-manager {
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
      }
      .toolbar {
        position: sticky;
        top: 0;
        z-index: 1;
      }
      .spacer {
        flex: 1;
      }
      .content {
        display: grid;
        grid-template-columns: 320px 1fr;
        gap: 2rem;
      }
      .sidebar {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }
      .sidebar mat-nav-list {
        border: 1px solid rgba(0, 0, 0, 0.12);
        border-radius: 8px;
        max-height: 70vh;
        overflow-y: auto;
      }
      .sidebar a.active {
        background-color: rgba(63, 81, 181, 0.1);
      }
      .details {
        display: grid;
        gap: 1.5rem;
      }
      .empty {
        display: flex;
        flex-direction: column;
        gap: 0.5rem;
        color: rgba(0, 0, 0, 0.6);
      }
      @media (max-width: 960px) {
        .content {
          grid-template-columns: 1fr;
        }
      }
    `
  ]
})
export class ThemeListComponent implements OnInit {
  private readonly themeService = inject(ThemeService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);

  private readonly themesSignal = signal<ThemeSummary[]>([]);
  private readonly selectedThemeSignal = signal<ThemeSummary | null>(null);
  protected readonly loading = signal(false);

  protected readonly themes = computed(() => this.themesSignal());
  protected readonly selectedTheme = computed(() => this.selectedThemeSignal());

  ngOnInit(): void {
    this.refreshThemes();
  }

  refreshThemes(): void {
    this.loading.set(true);
    this.themeService.getThemes().subscribe({
      next: themes => {
        this.themesSignal.set(themes);
        if (themes.length > 0) {
          const current = this.selectedThemeSignal();
          const match = current ? themes.find(t => t.id === current.id) : themes[0];
          this.selectedThemeSignal.set(match ?? null);
        } else {
          this.selectedThemeSignal.set(null);
        }
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  selectTheme(theme: ThemeSummary): void {
    this.selectedThemeSignal.set(theme);
  }

  handleThemeUploaded(theme: ThemeSummary): void {
    this.refreshThemes();
    this.selectedThemeSignal.set(theme);
  }

  handleActivation(tenantTheme: TenantTheme): void {
    this.refreshThemes();
  }

  openCloneDialog(): void {
    const dialogRef = this.dialog.open(ThemeCloneDialogComponent, {
      width: '520px'
    });

    dialogRef.afterClosed().subscribe(result => {
      if (result) {
        this.snackBar.open('Theme cloned successfully', 'Dismiss', { duration: 3000 });
        this.refreshThemes();
      }
    });
  }
}
