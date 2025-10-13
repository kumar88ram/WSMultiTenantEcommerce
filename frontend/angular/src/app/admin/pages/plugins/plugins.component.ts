import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatSlideToggleChange, MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatChipsModule } from '@angular/material/chips';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { AdminPlugin, AdminPluginService } from '../../services/admin-plugin.service';

@Component({
  selector: 'app-plugins',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatSlideToggleModule, MatChipsModule, MatButtonModule, MatProgressBarModule],
  template: `
    <section class="plugins">
      <header>
        <h1>Plugin catalogue</h1>
        <p>Enable extensions and integrations across all tenants.</p>
      </header>

      <div class="plugin-grid" *ngIf="!isLoading(); else loading">
        <mat-card *ngFor="let plugin of plugins()">
          <mat-card-header>
            <mat-card-title>{{ plugin.name }}</mat-card-title>
            <mat-card-subtitle>{{ plugin.category }}</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <p>{{ plugin.description }}</p>
            <div class="meta">
              <span>Version {{ plugin.version }}</span>
              <span class="chip" *ngIf="plugin.tags?.length">
                <mat-chip *ngFor="let tag of plugin.tags" color="primary" selected>{{ tag }}</mat-chip>
              </span>
            </div>
          </mat-card-content>
          <mat-card-actions>
            <mat-slide-toggle
              [checked]="plugin.enabled"
              [disabled]="pendingPluginId() === plugin.id"
              (change)="onToggle(plugin, $event)"
            >
              {{ plugin.enabled ? 'Enabled' : 'Disabled' }}
            </mat-slide-toggle>
            <span class="spacer"></span>
            <button mat-button>View changelog</button>
          </mat-card-actions>
        </mat-card>
      </div>
    </section>

    <ng-template #loading>
      <div class="loading">
        <mat-progress-bar mode="indeterminate"></mat-progress-bar>
        <p>Loading plugins…</p>
      </div>
    </ng-template>
  `,
  styles: [
    `
      header {
        margin-bottom: 2rem;
      }

      .plugin-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
        gap: 1.5rem;
      }

      mat-card-actions {
        display: flex;
        align-items: center;
        gap: 1rem;
      }

      .spacer {
        flex: 1 1 auto;
      }

      .meta {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-top: 1rem;
        color: rgba(0, 0, 0, 0.6);
      }

      .chip {
        display: flex;
        gap: 0.5rem;
      }

      .loading {
        display: grid;
        gap: 1rem;
      }
    `
  ]
})
export class PluginsComponent implements OnInit {
  private readonly pluginService = inject(AdminPluginService);

  protected readonly plugins = signal<AdminPlugin[]>([]);
  protected readonly isLoading = signal(true);
  protected readonly pendingPluginId = signal<string | null>(null);

  ngOnInit(): void {
    this.pluginService.getPlugins().subscribe({
      next: plugins => {
        this.plugins.set(plugins);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false)
    });
  }

  onToggle(plugin: AdminPlugin, change: MatSlideToggleChange): void {
    const previous = plugin.enabled;
    const nextState = change.checked;
    if (previous === nextState) {
      return;
    }

    this.pendingPluginId.set(plugin.id);
    plugin.enabled = nextState;

    this.pluginService.updatePluginState(plugin.id, nextState).subscribe({
      next: () => this.pendingPluginId.set(null),
      error: () => {
        plugin.enabled = previous;
        this.pendingPluginId.set(null);
      }
    });
  }
}
