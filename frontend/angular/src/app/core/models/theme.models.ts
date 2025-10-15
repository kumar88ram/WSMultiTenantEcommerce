export interface ThemeSection {
  id: string;
  sectionName: string;
  jsonConfig: string;
  sortOrder: number;
}

export interface ThemeSummary {
  id: string;
  name: string;
  code: string;
  version: string;
  description?: string;
  previewImageUrl?: string;
  isActive: boolean;
  createdAt: string;
  sections: ThemeSection[];
}

export interface ThemeVariable {
  key: string;
  value: string;
}

export interface TenantTheme {
  tenantThemeId: string;
  tenantId: string;
  theme: ThemeSummary;
  activatedAt: string;
  isActive: boolean;
  variables: ThemeVariable[];
}

export interface ThemeSectionsRequest {
  sections: Array<{
    sectionName: string;
    configuration: unknown;
    sortOrder: number;
  }>;
}

export interface ThemeVariablesRequest {
  variables: Record<string, string>;
}
