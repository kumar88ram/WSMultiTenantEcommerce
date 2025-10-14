import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { NgForOf } from '@angular/common';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatListModule } from '@angular/material/list';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatDividerModule } from '@angular/material/divider';

interface NavLink {
  label: string;
  icon: string;
  routerLink: string;
}

@Component({
  selector: 'app-admin-layout',
  standalone: true,
  imports: [
    RouterOutlet,
    RouterLink,
    RouterLinkActive,
    NgForOf,
    MatSidenavModule,
    MatToolbarModule,
    MatListModule,
    MatIconModule,
    MatButtonModule,
    MatDividerModule
  ],
  template: `
    <mat-sidenav-container class="admin-shell">
      <mat-sidenav mode="side" opened class="admin-shell__sidenav">
        <div class="branding">
          <h1>Admin</h1>
          <p class="subtitle">Platform control center</p>
        </div>

        <mat-divider></mat-divider>

        <mat-nav-list>
          <a
            mat-list-item
            *ngFor="let link of navLinks"
            [routerLink]="link.routerLink"
            routerLinkActive="active"
            [routerLinkActiveOptions]="{ exact: true }"
          >
            <mat-icon>{{ link.icon }}</mat-icon>
            <span>{{ link.label }}</span>
          </a>
        </mat-nav-list>

        <div class="sidenav-footer">
          <button mat-stroked-button color="primary" routerLink="/admin/tenants/create">
            Create tenant
          </button>
        </div>
      </mat-sidenav>

      <mat-sidenav-content class="admin-shell__content">
        <mat-toolbar color="primary" class="admin-shell__toolbar">
          <span>Administration</span>
          <span class="spacer"></span>
          <button mat-icon-button aria-label="Administrator account">
            <mat-icon>account_circle</mat-icon>
          </button>
        </mat-toolbar>

        <section class="content">
          <router-outlet></router-outlet>
        </section>
      </mat-sidenav-content>
    </mat-sidenav-container>
  `,
  styles: [
    `
      .admin-shell {
        height: 100vh;
      }

      .admin-shell__sidenav {
        width: 280px;
        padding: 1.5rem 1rem;
        box-sizing: border-box;
      }

      .branding {
        padding: 0 0.75rem 1rem;
      }

      .branding h1 {
        margin: 0;
        font-size: 1.5rem;
        font-weight: 600;
      }

      .subtitle {
        margin: 0;
        color: rgba(0, 0, 0, 0.54);
      }

      mat-nav-list {
        margin-top: 1rem;
        display: grid;
        gap: 0.5rem;
      }

      a.mat-mdc-list-item {
        border-radius: 0.75rem;
      }

      a.mat-mdc-list-item.active {
        background: rgba(33, 150, 243, 0.16);
      }

      a.mat-mdc-list-item mat-icon {
        margin-right: 1rem;
      }

      .sidenav-footer {
        margin-top: auto;
        padding: 1rem 0.75rem 0;
      }

      .admin-shell__content {
        background: #f4f6f8;
      }

      .admin-shell__toolbar {
        position: sticky;
        top: 0;
        z-index: 2;
      }

      .spacer {
        flex: 1 1 auto;
      }

      .content {
        padding: 2rem;
        box-sizing: border-box;
      }

      @media (max-width: 960px) {
        mat-sidenav-container, mat-sidenav {
          height: auto;
        }
      }
    `
  ]
})
export class AdminLayoutComponent {
  protected readonly navLinks: NavLink[] = [
    { label: 'Tenants', icon: 'groups', routerLink: '/admin/tenants' },
    { label: 'Plans', icon: 'view_quilt', routerLink: '/admin/plans' },
    { label: 'Orders', icon: 'shopping_cart', routerLink: '/admin/orders' },
    { label: 'Analytics', icon: 'insights', routerLink: '/admin/analytics' },
    { label: 'Plugins', icon: 'extension', routerLink: '/admin/plugins' }
  ];
}
