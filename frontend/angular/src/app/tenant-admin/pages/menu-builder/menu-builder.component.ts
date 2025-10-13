import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CdkDragDrop, DragDropModule, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Subscription } from 'rxjs';
import {
  MenuItem,
  TenantAdminMenuService
} from '../../services/tenant-admin-menu.service';

@Component({
  selector: 'app-menu-builder',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DragDropModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatProgressBarModule
  ],
  template: `
    <section class="menu-builder">
      <header>
        <div>
          <h1>Menu builder</h1>
          <p class="subtitle">Drag items to compose nested navigation for your storefront.</p>
        </div>
        <div class="actions">
          <button mat-stroked-button color="primary" (click)="addItem()">
            <mat-icon>add</mat-icon>
            Add top-level item
          </button>
          <button mat-flat-button color="accent" (click)="save()" [disabled]="saving()">
            <mat-icon>save</mat-icon>
            Save menu
          </button>
        </div>
      </header>

      <mat-card class="menu-card">
        <mat-card-content>
          <div class="menu-drop" cdkDropListGroup>
            <div
              cdkDropList
              [cdkDropListData]="menuItems()"
              (cdkDropListDropped)="drop($event)"
              class="menu-list"
            >
              <div class="menu-item" *ngFor="let item of menuItems(); trackBy: trackById" cdkDrag>
                <div class="item-body">
                  <div class="handle" cdkDragHandle>
                    <mat-icon>drag_indicator</mat-icon>
                  </div>
                  <div class="fields">
                    <label>
                      <span>Label</span>
                      <input type="text" [(ngModel)]="item.label" placeholder="Menu item label" />
                    </label>
                    <label>
                      <span>URL</span>
                      <input type="text" [(ngModel)]="item.url" placeholder="/collections/new" />
                    </label>
                  </div>
                  <div class="item-actions">
                    <button mat-icon-button type="button" (click)="addItem(item)">
                      <mat-icon>subdirectory_arrow_right</mat-icon>
                    </button>
                    <button mat-icon-button color="warn" type="button" (click)="removeItem(item.id)">
                      <mat-icon>delete</mat-icon>
                    </button>
                  </div>
                </div>

                <div
                  class="children"
                  cdkDropList
                  [cdkDropListData]="item.children"
                  (cdkDropListDropped)="drop($event, item)"
                >
                  <div class="menu-item" *ngFor="let child of item.children; trackBy: trackById" cdkDrag>
                    <div class="item-body">
                      <div class="handle" cdkDragHandle>
                        <mat-icon>drag_indicator</mat-icon>
                      </div>
                      <div class="fields">
                        <label>
                          <span>Label</span>
                          <input type="text" [(ngModel)]="child.label" placeholder="Menu item label" />
                        </label>
                        <label>
                          <span>URL</span>
                          <input type="text" [(ngModel)]="child.url" placeholder="/collections/new" />
                        </label>
                      </div>
                      <div class="item-actions">
                        <button mat-icon-button type="button" (click)="addItem(child)">
                          <mat-icon>subdirectory_arrow_right</mat-icon>
                        </button>
                        <button mat-icon-button color="warn" type="button" (click)="removeItem(child.id)">
                          <mat-icon>delete</mat-icon>
                        </button>
                      </div>
                    </div>

                    <div
                      class="children"
                      cdkDropList
                      [cdkDropListData]="child.children"
                      (cdkDropListDropped)="drop($event, child)"
                    ></div>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </mat-card-content>

        <mat-progress-bar *ngIf="saving()" mode="indeterminate"></mat-progress-bar>
      </mat-card>

      <section class="preview">
        <h2>Menu JSON preview</h2>
        <pre>{{ menuItems() | json }}</pre>
      </section>
    </section>
  `,
  styles: [
    `
      .menu-builder {
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
      }

      header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        flex-wrap: wrap;
        gap: 1rem;
      }

      .subtitle {
        color: rgba(0, 0, 0, 0.6);
        margin: 0;
      }

      .actions {
        display: flex;
        gap: 0.5rem;
        align-items: center;
      }

      .menu-card {
        padding: 1rem;
      }

      .menu-drop {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }

      .menu-list,
      .children {
        display: flex;
        flex-direction: column;
        gap: 0.75rem;
        min-height: 40px;
      }

      .menu-item {
        border: 1px solid rgba(0, 0, 0, 0.12);
        border-radius: 0.75rem;
        background: white;
        box-shadow: 0 2px 12px rgba(0, 0, 0, 0.06);
      }

      .item-body {
        display: grid;
        grid-template-columns: auto 1fr auto;
        gap: 1rem;
        padding: 0.75rem 1rem;
        align-items: center;
      }

      .handle {
        color: rgba(0, 0, 0, 0.4);
        cursor: grab;
      }

      .fields {
        display: grid;
        gap: 0.5rem;
      }

      label {
        display: grid;
        gap: 0.25rem;
        font-size: 0.8rem;
        color: rgba(0, 0, 0, 0.6);
      }

      input[type='text'] {
        border: 1px solid rgba(0, 0, 0, 0.12);
        border-radius: 0.5rem;
        padding: 0.5rem 0.75rem;
        font-size: 0.95rem;
      }

      .item-actions {
        display: flex;
        align-items: center;
      }

      .children {
        margin-left: 3rem;
        padding-bottom: 0.75rem;
      }

      .preview pre {
        background: rgba(0, 0, 0, 0.04);
        border-radius: 0.75rem;
        padding: 1rem;
        overflow: auto;
      }

      @media (max-width: 960px) {
        .item-body {
          grid-template-columns: 1fr;
        }

        .children {
          margin-left: 1.5rem;
        }
      }
    `
  ]
})
export class MenuBuilderComponent implements OnInit, OnDestroy {
  private readonly menuService = inject(TenantAdminMenuService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly subscription = new Subscription();

  protected readonly menuItems = signal<MenuItem[]>([]);
  protected readonly saving = signal(false);

  ngOnInit(): void {
    const loadSub = this.menuService
      .getMenu()
      .subscribe(menu => this.menuItems.set(this.normalize(menu)));
    this.subscription.add(loadSub);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  trackById = (_: number, item: MenuItem) => item.id;

  addItem(parent?: MenuItem): void {
    const newItem: MenuItem = {
      id: this.generateId(),
      label: 'New item',
      url: '/',
      children: []
    };

    if (!parent) {
      this.menuItems.update(items => [...items, newItem]);
      return;
    }

    parent.children = parent.children ?? [];
    parent.children.push(newItem);
    this.menuItems.set([...this.menuItems()]);
  }

  removeItem(id: string): void {
    const removeRecursive = (items: MenuItem[]): MenuItem[] =>
      items
        .filter(item => item.id !== id)
        .map(item => ({ ...item, children: item.children ? removeRecursive(item.children) : [] }));

    this.menuItems.set(removeRecursive(this.menuItems()));
  }

  drop(event: CdkDragDrop<MenuItem[]>, parent?: MenuItem): void {
    if (!event.container.data || !event.previousContainer.data) {
      return;
    }

    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    } else {
      transferArrayItem(event.previousContainer.data, event.container.data, event.previousIndex, event.currentIndex);
    }

    if (parent) {
      parent.children = [...(event.container.data ?? [])];
    }

    this.menuItems.set([...this.menuItems()]);
  }

  save(): void {
    this.saving.set(true);
    const saveSub = this.menuService.saveMenu(this.menuItems()).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Menu saved', 'Close', { duration: 2000 });
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Failed to save menu', 'Close', { duration: 3000 });
      }
    });

    this.subscription.add(saveSub);
  }

  private generateId(): string {
    return typeof crypto !== 'undefined' && 'randomUUID' in crypto
      ? crypto.randomUUID()
      : Math.random().toString(36).slice(2, 10);
  }

  private normalize(items: MenuItem[]): MenuItem[] {
    return items.map(item => ({
      ...item,
      children: item.children ? this.normalize(item.children) : []
    }));
  }
}
