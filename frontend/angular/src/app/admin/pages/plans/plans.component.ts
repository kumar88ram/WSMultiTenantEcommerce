import { Component, inject } from '@angular/core';
import { CommonModule, CurrencyPipe } from '@angular/common';
import { Observable } from 'rxjs';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { AdminPlan, AdminPlanService } from '../../services/admin-plan.service';

@Component({
  selector: 'app-plans',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, MatCardModule, MatButtonModule, MatIconModule, MatChipsModule],
  template: `
    <section class="plans">
      <header>
        <h1>Subscription plans</h1>
        <p>Define billing tiers and entitlements available to tenants.</p>
      </header>

      <div class="plan-grid" *ngIf="plans$ | async as plans">
        <mat-card *ngFor="let plan of plans" [class.highlighted]="plan.recommended">
          <mat-card-header>
            <mat-card-title>{{ plan.name }}</mat-card-title>
            <mat-card-subtitle>{{ plan.interval | titlecase }} billing</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="price">
              <span class="amount">{{ plan.price | currency }}</span>
              <span class="per">/ {{ plan.interval }}</span>
            </div>

            <ul>
              <li *ngFor="let feature of plan.features">
                <mat-icon color="primary">check_circle</mat-icon>
                <span>{{ feature }}</span>
              </li>
            </ul>

            <div class="tags" *ngIf="plan.tags?.length">
              <mat-chip *ngFor="let tag of plan.tags" color="primary" selected>{{ tag }}</mat-chip>
            </div>
          </mat-card-content>
          <mat-card-actions>
            <button mat-stroked-button color="primary">Edit plan</button>
            <button mat-button>Preview upgrade</button>
          </mat-card-actions>
        </mat-card>
      </div>
    </section>
  `,
  styles: [
    `
      header {
        margin-bottom: 2rem;
      }

      .plan-grid {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(260px, 1fr));
        gap: 1.5rem;
      }

      mat-card.highlighted {
        border: 2px solid var(--mdc-theme-primary, #1976d2);
      }

      .price {
        display: flex;
        align-items: baseline;
        gap: 0.5rem;
        margin-bottom: 1rem;
      }

      .price .amount {
        font-size: 2rem;
        font-weight: 600;
      }

      .price .per {
        color: rgba(0, 0, 0, 0.6);
      }

      ul {
        list-style: none;
        padding: 0;
        margin: 0;
        display: grid;
        gap: 0.75rem;
      }

      li {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        color: rgba(0, 0, 0, 0.8);
      }

      mat-card-actions {
        display: flex;
        gap: 0.75rem;
        padding-bottom: 1.5rem;
      }

      .tags {
        margin-top: 1.5rem;
        display: flex;
        gap: 0.5rem;
        flex-wrap: wrap;
      }
    `
  ]
})
export class PlansComponent {
  private readonly planService = inject(AdminPlanService);
  protected readonly plans$: Observable<AdminPlan[]> = this.planService.getPlans();
}
