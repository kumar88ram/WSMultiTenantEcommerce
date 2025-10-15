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

export interface ThemePreviewResponse {
  previewUrl: string;
  expiresAt: string;
}

export interface ThemeCloneRequest {
  sourceTenantId: string;
  targetTenantId: string;
}

export interface ThemeCloneResponse {
  tenantThemeId: string;
}

export interface ThemeAnalyticsSummary {
  themeId: string;
  themeName: string;
  activeTenantsCount: number;
  averageActiveDays: number;
  topTenants: TenantUsageSnapshot[];
}

export interface TenantUsageSnapshot {
  tenantId: string;
  totalActiveDays: number;
  activatedAt?: string | null;
}

export interface TenantThemeUsage {
  tenantId: string;
  themeId: string;
  isActive: boolean;
  activatedAt: string;
  deactivatedAt?: string | null;
  totalActiveDays: number;
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
