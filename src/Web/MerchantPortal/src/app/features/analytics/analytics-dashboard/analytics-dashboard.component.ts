import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-analytics-dashboard',
  standalone: true,
  imports: [CommonModule, MatCardModule, MatIconModule],
  template: `
    <div class="analytics-container">
      <h1>Analytics Dashboard</h1>
      <div class="metrics-grid">
        @for (metric of metrics(); track metric.id) {
          <mat-card class="metric-card">
            <mat-card-content>
              <div class="metric-header">
                <mat-icon>{{metric.icon}}</mat-icon>
                <h3>{{metric.title}}</h3>
              </div>
              <div class="metric-value">{{metric.value}}</div>
              <div class="metric-change" [class]="metric.changeType">
                {{metric.change}}
              </div>
            </mat-card-content>
          </mat-card>
        }
      </div>
    </div>
  `,
  styles: [`
    .analytics-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .metrics-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 1.5rem;
    }

    .metric-card {
      text-align: center;
    }

    .metric-header {
      display: flex;
      align-items: center;
      justify-content: center;
      gap: 0.5rem;
      margin-bottom: 1rem;
      
      mat-icon {
        color: #1976d2;
      }
    }

    .metric-value {
      font-size: 2rem;
      font-weight: bold;
      color: #333;
      margin-bottom: 0.5rem;
    }

    .metric-change {
      font-size: 0.9rem;
      
      &.positive {
        color: #4caf50;
      }
      
      &.negative {
        color: #f44336;
      }
    }
  `]
})
export class AnalyticsDashboardComponent implements OnInit {
  metrics = signal([
    { id: 1, title: 'Total Sales', value: '1,234,567 NOK', change: '+12.5%', changeType: 'positive', icon: 'trending_up' },
    { id: 2, title: 'BNPL Orders', value: '456', change: '+8.3%', changeType: 'positive', icon: 'shopping_cart' },
    { id: 3, title: 'Conversion Rate', value: '3.2%', change: '-0.5%', changeType: 'negative', icon: 'analytics' },
    { id: 4, title: 'Average Order', value: '2,850 NOK', change: '+5.1%', changeType: 'positive', icon: 'attach_money' }
  ]);

  ngOnInit() {
    // TODO: Load analytics data
  }
}