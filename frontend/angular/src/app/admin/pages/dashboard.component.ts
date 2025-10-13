import { Component } from '@angular/core';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  template: `
    <section class="dashboard">
      <h1>Admin Dashboard</h1>
      <p>Secure tenant-specific management area.</p>
    </section>
  `
})
export class DashboardComponent {}
