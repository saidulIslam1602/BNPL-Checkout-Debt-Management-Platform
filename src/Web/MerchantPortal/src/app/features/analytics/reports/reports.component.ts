import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-reports',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div class="reports-container">
      <h1>Reports</h1>
      <mat-card>
        <mat-card-content>
          <p>Reports functionality coming soon...</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .reports-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }
  `]
})
export class ReportsComponent {}