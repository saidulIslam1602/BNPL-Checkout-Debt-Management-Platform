import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-risk-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div class="risk-dashboard-container">
      <h1>Risk Management</h1>
      <mat-card>
        <mat-card-content>
          <p>Risk management dashboard coming soon...</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .risk-dashboard-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }
  `]
})
export class RiskDashboardComponent {}