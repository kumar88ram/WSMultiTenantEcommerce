import { NgModule } from '@angular/core';
import { RouterModule } from '@angular/router';
import { TENANT_ADMIN_ROUTES } from './tenant-admin.routes';

@NgModule({
  imports: [RouterModule.forChild(TENANT_ADMIN_ROUTES)],
  exports: [RouterModule]
})
export class TenantAdminModule {}
