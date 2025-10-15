import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, OnChanges, Output, SimpleChanges, inject } from '@angular/core';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatListModule } from '@angular/material/list';
import { ThemeService } from '../../../core/services/theme.service';
import { ThemeSummary } from '../../../core/models/theme.models';

@Component({
  selector: 'app-theme-section-builder',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatListModule
  ],
  template: `
    <section class="section-builder" *ngIf="theme">
      <header>
        <h3>Layout Sections</h3>
        <button mat-stroked-button color="primary" type="button" (click)="addSection()">
          <mat-icon>add</mat-icon>
          Add Section
        </button>
      </header>
      <div class="error" *ngIf="errorMessage">{{ errorMessage }}</div>
      <form [formGroup]="form" (ngSubmit)="save()">
        <div formArrayName="sections" class="sections">
          <div class="section-card" *ngFor="let section of sections.controls; index as i" [formGroupName]="i">
            <div class="section-card__header">
              <h4>Section {{ i + 1 }}</h4>
              <button mat-icon-button color="warn" type="button" (click)="removeSection(i)">
                <mat-icon>delete</mat-icon>
              </button>
            </div>
            <mat-form-field appearance="outline">
              <mat-label>Section Name</mat-label>
              <input matInput formControlName="sectionName" />
            </mat-form-field>
            <mat-form-field appearance="outline">
              <mat-label>Sort Order</mat-label>
              <input matInput type="number" formControlName="sortOrder" />
            </mat-form-field>
            <mat-form-field appearance="outline" class="config-field">
              <mat-label>Configuration (JSON)</mat-label>
              <textarea matInput rows="6" formControlName="configuration"></textarea>
            </mat-form-field>
          </div>
        </div>
        <div class="actions">
          <button mat-flat-button color="primary" type="submit" [disabled]="form.invalid || working">
            Save Layout
          </button>
        </div>
      </form>
    </section>
  `,
  styles: [
    `
      .section-builder {
        display: flex;
        flex-direction: column;
        gap: 1rem;
      }
      header {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }
      .sections {
        display: grid;
        gap: 1rem;
      }
      .section-card {
        border: 1px solid rgba(0, 0, 0, 0.12);
        border-radius: 8px;
        padding: 1rem;
        display: grid;
        gap: 0.75rem;
      }
      .section-card__header {
        display: flex;
        justify-content: space-between;
        align-items: center;
      }
      .config-field textarea {
        font-family: 'Fira Code', monospace;
      }
      .actions {
        display: flex;
        justify-content: flex-end;
      }
      .error {
        color: #d32f2f;
      }
    `
  ]
})
export class ThemeSectionBuilderComponent implements OnChanges {
  @Input() theme: ThemeSummary | null = null;
  @Output() readonly sectionsSaved = new EventEmitter<void>();

  protected working = false;
  protected errorMessage = '';

  private readonly fb = inject(FormBuilder);
  private readonly themeService = inject(ThemeService);

  protected readonly form = this.fb.group({
    sections: this.fb.array([])
  });

  get sections(): FormArray {
    return this.form.get('sections') as FormArray;
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['theme']) {
      this.loadSections();
    }
  }

  addSection(): void {
    this.sections.push(
      this.fb.group({
        sectionName: ['', Validators.required],
        sortOrder: [this.sections.length, Validators.required],
        configuration: ['{}', Validators.required]
      })
    );
  }

  removeSection(index: number): void {
    this.sections.removeAt(index);
  }

  save(): void {
    if (!this.theme) {
      return;
    }

    const payload: { sectionName: string; configuration: unknown; sortOrder: number }[] = [];

    for (const control of this.sections.controls) {
      const value = control.value as { sectionName: string; configuration: string; sortOrder: number };
      try {
        const parsed = JSON.parse(value.configuration ?? '{}');
        payload.push({ sectionName: value.sectionName, configuration: parsed, sortOrder: value.sortOrder });
        this.errorMessage = '';
      } catch (error) {
        this.errorMessage = `Invalid JSON in section "${value.sectionName}"`;
        return;
      }
    }

    this.working = true;
    this.themeService
      .saveSections(this.theme.id, { sections: payload })
      .subscribe({
        next: () => {
          this.working = false;
          this.sectionsSaved.emit();
        },
        error: () => {
          this.working = false;
          this.errorMessage = 'Failed to save sections. Please try again.';
        }
      });
  }

  private loadSections(): void {
    this.sections.clear();
    if (!this.theme) {
      return;
    }

    for (const section of this.theme.sections ?? []) {
      let formattedConfig = section.jsonConfig;
      try {
        formattedConfig = JSON.stringify(JSON.parse(section.jsonConfig), null, 2);
      } catch {
        formattedConfig = section.jsonConfig;
      }

      this.sections.push(
        this.fb.group({
          sectionName: [section.sectionName, Validators.required],
          sortOrder: [section.sortOrder, Validators.required],
          configuration: [formattedConfig, Validators.required]
        })
      );
    }

    if (this.sections.length === 0) {
      this.addSection();
    }
  }
}
