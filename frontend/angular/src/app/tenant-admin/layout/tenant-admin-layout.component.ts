import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { CommonModule } from '@angular/common';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';
import { MatTooltipModule } from '@angular/material/tooltip';

@Component({
  selector: 'app-tenant-admin-layout',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule,
    MatTooltipModule
  ],
  template: `
    <div class="tenant-admin-layout">
      <mat-toolbar color="primary" class="toolbar">
        <span>Tenant admin</span>
        <span class="spacer"></span>
        <button mat-icon-button [routerLink]="['/admin']" matTooltip="Platform admin">
          <mat-icon>dashboard</mat-icon>
        </button>
      </mat-toolbar>

      <div class="layout-body">
        <nav class="sidebar">
          <mat-nav-list>
            <a mat-list-item routerLink="products" routerLinkActive="active">Products</a>
            <a mat-list-item routerLink="menus" routerLinkActive="active">Menus</a>
            <a mat-list-item routerLink="forms" routerLinkActive="active">Forms</a>
            <a mat-list-item routerLink="settings" routerLinkActive="active">Settings</a>
          </mat-nav-list>
          <mat-divider></mat-divider>
          <div class="sidebar-footer">
            <small>Manage your storefront content, menus and configuration.</small>
          </div>
        </nav>

        <main class="content">
          <router-outlet></router-outlet>
        </main>
      </div>
    </div>
  `,
  styles: [
    `
      .tenant-admin-layout {
        display: flex;
        flex-direction: column;
        height: 100vh;
      }

      .toolbar {
        position: sticky;
        top: 0;
        z-index: 2;
      }

      .spacer {
        flex: 1;
      }

      .layout-body {
        display: grid;
        grid-template-columns: 260px 1fr;
        flex: 1;
        overflow: hidden;
      }

      .sidebar {
        border-right: 1px solid rgba(0, 0, 0, 0.12);
        padding: 1rem;
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .sidebar-footer {
        color: rgba(0, 0, 0, 0.6);
        font-size: 0.8rem;
      }

      .content {
        padding: 2rem;
        overflow-y: auto;
      }

      a.active {
        font-weight: 600;
      }

      @media (max-width: 960px) {
        .layout-body {
          grid-template-columns: 1fr;
        }

        .sidebar {
          flex-direction: row;
          align-items: center;
          gap: 0.5rem;
          border-right: none;
          border-bottom: 1px solid rgba(0, 0, 0, 0.12);
        }

        .sidebar mat-nav-list {
          display: flex;
          width: 100%;
        }

        .sidebar mat-list-item {
          flex: 1;
          justify-content: center;
        }
      }
    `
  ]
})
export class TenantAdminLayoutComponent {}
