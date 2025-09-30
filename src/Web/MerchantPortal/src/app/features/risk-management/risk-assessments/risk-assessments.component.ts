import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';

@Component({
  selector: 'app-risk-assessments',
  standalone: true,
  imports: [CommonModule, MatCardModule],
  template: `
    <div class="risk-assessments-container">
      <h1>Risk Assessments</h1>
      <mat-card>
        <mat-card-content>
          <p>Risk assessments functionality coming soon...</p>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styles: [`
    .risk-assessments-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }
  `]
})
export class RiskAssessmentsComponent {}