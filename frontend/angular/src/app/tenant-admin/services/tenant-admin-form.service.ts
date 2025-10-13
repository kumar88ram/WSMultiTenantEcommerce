import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { environment } from '../../../../environments/environment';

export type FormFieldType = 'text' | 'textarea' | 'select' | 'checkbox' | 'file';

export interface FormFieldConfig {
  id: string;
  type: FormFieldType;
  label: string;
  required: boolean;
  placeholder?: string;
  options?: string[];
}

export interface FormBuilderDefinition {
  fields: FormFieldConfig[];
}

@Injectable({ providedIn: 'root' })
export class TenantAdminFormService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/tenant/forms`;

  getForm(): Observable<FormBuilderDefinition> {
    return this.http
      .get<FormBuilderDefinition>(this.baseUrl)
      .pipe(map(definition => ({ fields: definition.fields ?? [] })));
  }

  saveForm(definition: FormBuilderDefinition): Observable<void> {
    return this.http.put<void>(this.baseUrl, definition);
  }
}
