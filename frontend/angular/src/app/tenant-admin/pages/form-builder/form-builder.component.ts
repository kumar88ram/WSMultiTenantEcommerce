import { CommonModule } from '@angular/common';
import { Component, OnDestroy, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CdkDragDrop, DragDropModule, moveItemInArray } from '@angular/cdk/drag-drop';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBar, MatSnackBarModule } from '@angular/material/snack-bar';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { Subscription } from 'rxjs';
import {
  FormBuilderDefinition,
  FormFieldConfig,
  FormFieldType,
  TenantAdminFormService
} from '../../services/tenant-admin-form.service';

interface FieldPaletteItem {
  type: FormFieldType;
  label: string;
  description: string;
}

@Component({
  selector: 'app-form-builder',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    DragDropModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatSnackBarModule,
    MatSlideToggleModule,
    MatInputModule,
    MatProgressBarModule
  ],
  template: `
    <section class="form-builder">
      <header>
        <div>
          <h1>Form builder</h1>
          <p class="subtitle">Drag fields from the palette to assemble custom lead capture forms.</p>
        </div>
        <button mat-flat-button color="primary" (click)="save()" [disabled]="saving()">
          <mat-icon>save</mat-icon>
          Save form
        </button>
      </header>

      <div class="workspace">
        <mat-card class="palette">
          <mat-card-header>
            <mat-card-title>Field palette</mat-card-title>
            <mat-card-subtitle>Drag any element into the canvas.</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="palette-items" cdkDropList [cdkDropListData]="palette" cdkDropListDisabled>
              <article
                class="palette-item"
                *ngFor="let item of palette"
                cdkDrag
                [cdkDragData]="item"
              >
                <div>
                  <h3>{{ item.label }}</h3>
                  <p>{{ item.description }}</p>
                </div>
                <mat-icon>drag_indicator</mat-icon>
              </article>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card class="canvas" cdkDropList [cdkDropListData]="fields()" (cdkDropListDropped)="drop($event)">
          <mat-card-header>
            <mat-card-title>Form canvas</mat-card-title>
            <mat-card-subtitle>Reorder fields to define the response flow.</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <p class="empty" *ngIf="fields().length === 0">Drag a field type from the palette to get started.</p>

            <div class="field" *ngFor="let field of fields(); trackBy: trackById" cdkDrag (click)="selectField(field)" [class.active]="selectedField() && selectedField()!.id === field.id">
              <div class="field-header">
                <div class="field-meta">
                  <mat-icon>{{ iconFor(field.type) }}</mat-icon>
                  <div>
                    <h4>{{ field.label }}</h4>
                    <small>{{ field.type }}</small>
                  </div>
                </div>
                <div class="field-actions">
                  <button mat-icon-button type="button" (click)="selectField(field)">
                    <mat-icon>tune</mat-icon>
                  </button>
                  <button mat-icon-button color="warn" type="button" (click)="removeField(field.id); $event.stopPropagation();">
                    <mat-icon>delete</mat-icon>
                  </button>
                </div>
              </div>
              <p class="helper">{{ field.required ? 'Required' : 'Optional' }}</p>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card class="inspector" *ngIf="selectedField() as current">
          <mat-card-header>
            <mat-card-title>Field settings</mat-card-title>
            <mat-card-subtitle>Configure the selected field.</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <label>
              <span>Label</span>
              <input type="text" [(ngModel)]="current.label" />
            </label>
            <label>
              <span>Placeholder</span>
              <input type="text" [(ngModel)]="current.placeholder" />
            </label>
            <label class="toggle">
              <span>Required</span>
              <mat-slide-toggle [(ngModel)]="current.required"></mat-slide-toggle>
            </label>

            <ng-container [ngSwitch]="current.type">
              <div *ngSwitchCase="'select'" class="options-editor">
                <span>Options (comma separated)</span>
                <textarea [(ngModel)]="selectOptions" (ngModelChange)="updateSelectOptions($event)"></textarea>
              </div>
            </ng-container>
          </mat-card-content>
        </mat-card>
      </div>

      <section class="preview">
        <h2>JSON definition</h2>
        <pre>{{ fields() | json }}</pre>
      </section>

      <mat-progress-bar *ngIf="saving()" mode="indeterminate"></mat-progress-bar>
    </section>
  `,
  styles: [
    `
      .form-builder {
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

      .workspace {
        display: grid;
        grid-template-columns: minmax(240px, 280px) 1fr minmax(260px, 320px);
        gap: 1.5rem;
        align-items: start;
      }

      .palette-items {
        display: grid;
        gap: 0.75rem;
      }

      .palette-item {
        border: 1px dashed rgba(0, 0, 0, 0.2);
        border-radius: 0.75rem;
        padding: 1rem;
        display: flex;
        justify-content: space-between;
        align-items: center;
        gap: 1rem;
        cursor: grab;
      }

      .palette-item h3 {
        margin: 0;
      }

      .canvas {
        min-height: 400px;
      }

      .canvas .field {
        border: 1px solid rgba(0, 0, 0, 0.12);
        border-radius: 0.75rem;
        padding: 1rem;
        margin-bottom: 1rem;
        box-shadow: 0 2px 12px rgba(0, 0, 0, 0.04);
        cursor: pointer;
      }

      .canvas .field.active {
        border-color: rgba(63, 81, 181, 0.8);
        box-shadow: 0 0 0 2px rgba(63, 81, 181, 0.2);
      }

      .field-header {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }

      .field-meta {
        display: flex;
        gap: 0.75rem;
        align-items: center;
      }

      .field-meta mat-icon {
        background: rgba(63, 81, 181, 0.1);
        border-radius: 50%;
        padding: 0.5rem;
      }

      .field-actions {
        display: flex;
        align-items: center;
      }

      .helper {
        margin: 0.75rem 0 0;
        color: rgba(0, 0, 0, 0.6);
      }

      .inspector label {
        display: flex;
        flex-direction: column;
        gap: 0.25rem;
        margin-bottom: 1rem;
        font-size: 0.85rem;
        color: rgba(0, 0, 0, 0.6);
      }

      .inspector input,
      .inspector textarea {
        border: 1px solid rgba(0, 0, 0, 0.2);
        border-radius: 0.5rem;
        padding: 0.5rem 0.75rem;
        font-size: 0.95rem;
      }

      .options-editor textarea {
        min-height: 120px;
      }

      .empty {
        text-align: center;
        color: rgba(0, 0, 0, 0.54);
      }

      @media (max-width: 1280px) {
        .workspace {
          grid-template-columns: 1fr;
        }

        .inspector {
          order: 3;
        }
      }
    `
  ]
})
export class FormBuilderComponent implements OnInit, OnDestroy {
  private readonly formService = inject(TenantAdminFormService);
  private readonly snackBar = inject(MatSnackBar);
  private readonly subscription = new Subscription();

  protected readonly palette: FieldPaletteItem[] = [
    { type: 'text', label: 'Text input', description: 'Single line of text.' },
    { type: 'textarea', label: 'Paragraph', description: 'Multi-line text input.' },
    { type: 'select', label: 'Dropdown', description: 'Choose a value from predefined options.' },
    { type: 'checkbox', label: 'Checkbox', description: 'True/false selection.' },
    { type: 'file', label: 'File upload', description: 'Allow customers to attach files.' }
  ];

  protected readonly fields = signal<FormFieldConfig[]>([]);
  protected readonly selectedField = signal<FormFieldConfig | null>(null);
  protected readonly saving = signal(false);
  protected selectOptions = '';

  ngOnInit(): void {
    const loadSub = this.formService.getForm().subscribe(definition => {
      this.fields.set(definition.fields);
      if (definition.fields.length > 0) {
        this.selectField(definition.fields[0]);
      }
    });

    this.subscription.add(loadSub);
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  trackById = (_: number, field: FormFieldConfig) => field.id;

  iconFor(type: FormFieldType): string {
    switch (type) {
      case 'text':
        return 'title';
      case 'textarea':
        return 'subject';
      case 'select':
        return 'arrow_drop_down_circle';
      case 'checkbox':
        return 'check_box';
      case 'file':
        return 'attach_file';
      default:
        return 'drag_indicator';
    }
  }

  selectField(field: FormFieldConfig): void {
    this.selectedField.set(field);
    if (field.type === 'select') {
      this.selectOptions = field.options?.join(', ') ?? '';
    } else {
      this.selectOptions = '';
    }
  }

  removeField(id: string): void {
    const remaining = this.fields().filter(field => field.id !== id);
    this.fields.set(remaining);
    if (this.selectedField()?.id === id) {
      this.selectedField.set(remaining[0] ?? null);
    }
  }

  drop(event: CdkDragDrop<FormFieldConfig[] | FieldPaletteItem[]>): void {
    if (event.previousContainer === event.container) {
      moveItemInArray(event.container.data as FormFieldConfig[], event.previousIndex, event.currentIndex);
      this.fields.set([...(event.container.data as FormFieldConfig[])]);
      return;
    }

    const paletteItem = event.item.data as FieldPaletteItem;
    const newField: FormFieldConfig = {
      id: this.generateId(),
      type: paletteItem.type,
      label: paletteItem.label,
      required: false,
      placeholder: '',
      options: paletteItem.type === 'select' ? ['Option 1', 'Option 2'] : undefined
    };

    const updated = [...this.fields()];
    updated.splice(event.currentIndex, 0, newField);
    this.fields.set(updated);
    this.selectField(newField);
  }

  updateSelectOptions(value: string): void {
    this.selectOptions = value;
    const current = this.selectedField();
    if (current && current.type === 'select') {
      current.options = value
        .split(',')
        .map(option => option.trim())
        .filter(option => option.length > 0);
      this.fields.set([...this.fields()]);
    }
  }

  save(): void {
    this.saving.set(true);
    const definition: FormBuilderDefinition = {
      fields: this.fields()
    };

    const saveSub = this.formService.saveForm(definition).subscribe({
      next: () => {
        this.saving.set(false);
        this.snackBar.open('Form saved', 'Close', { duration: 2000 });
      },
      error: () => {
        this.saving.set(false);
        this.snackBar.open('Failed to save form', 'Close', { duration: 3000 });
      }
    });

    this.subscription.add(saveSub);
  }

  private generateId(): string {
    return crypto.randomUUID ? crypto.randomUUID() : Math.random().toString(36).slice(2, 10);
  }
}
