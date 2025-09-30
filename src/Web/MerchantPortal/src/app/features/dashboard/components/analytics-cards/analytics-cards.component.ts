import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Observable, interval, map, startWith } from 'rxjs';
import { AnalyticsService } from '../../../../core/services/analytics.service';
import { CurrencyPipe, DecimalPipe, PercentPipe } from '@angular/common';

export interface AnalyticsCard {
  title: string;
  value: number | string;
  previousValue?: number;
  change?: number;
  changeType?: 'increase' | 'decrease' | 'neutral';
  icon: string;
  color: 'primary' | 'accent' | 'warn' | 'success';
  format: 'currency' | 'number' | 'percent';
  description?: string;
  trend?: number[];
}

@Component({
  selector: 'app-analytics-cards',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressBarModule,
    MatTooltipModule,
    CurrencyPipe,
    DecimalPipe,
    PercentPipe
  ],
  template: `
    <div class="analytics-cards-container">
      <div class="cards-grid">
        <mat-card 
          *ngFor="let card of analyticsCards$ | async" 
          class="analytics-card"
          [ngClass]="'card-' + card.color">
          
          <mat-card-header class="card-header">
            <div class="card-icon">
              <mat-icon [ngClass]="'icon-' + card.color">{{ card.icon }}</mat-icon>
            </div>
            <div class="card-title-section">
              <mat-card-title class="card-title">{{ card.title }}</mat-card-title>
              <mat-card-subtitle 
                *ngIf="card.description" 
                class="card-description"
                [matTooltip]="card.description">
                {{ card.description }}
              </mat-card-subtitle>
            </div>
          </mat-card-header>

          <mat-card-content class="card-content">
            <div class="value-section">
              <div class="main-value">
                <span 
                  [ngSwitch]="card.format"
                  class="value-text">
                  <span *ngSwitchCase="'currency'">{{ card.value | currency:'NOK':'symbol':'1.0-0' }}</span>
                  <span *ngSwitchCase="'percent'">{{ card.value | percent:'1.1-1' }}</span>
                  <span *ngSwitchDefault>{{ card.value | number:'1.0-0' }}</span>
                </span>
              </div>
              
              <div 
                *ngIf="card.change !== undefined" 
                class="change-indicator"
                [ngClass]="'change-' + card.changeType">
                <mat-icon class="change-icon">
                  {{ card.changeType === 'increase' ? 'trending_up' : 
                     card.changeType === 'decrease' ? 'trending_down' : 'trending_flat' }}
                </mat-icon>
                <span class="change-value">
                  {{ card.change | percent:'1.1-1' }}
                </span>
              </div>
            </div>

            <!-- Mini trend chart -->
            <div *ngIf="card.trend && card.trend.length > 0" class="trend-chart">
              <svg width="100%" height="30" viewBox="0 0 100 30">
                <polyline
                  [attr.points]="getTrendPoints(card.trend)"
                  fill="none"
                  [attr.stroke]="getTrendColor(card.color)"
                  stroke-width="2"
                  class="trend-line">
                </polyline>
              </svg>
            </div>
          </mat-card-content>

          <mat-card-actions class="card-actions">
            <button 
              mat-button 
              color="primary" 
              (click)="viewDetails(card.title)"
              class="details-button">
              <mat-icon>visibility</mat-icon>
              View Details
            </button>
          </mat-card-actions>
        </mat-card>
      </div>

      <!-- Real-time update indicator -->
      <div class="update-indicator">
        <mat-icon class="pulse-icon">sync</mat-icon>
        <span class="update-text">Last updated: {{ lastUpdated$ | async | date:'short' }}</span>
      </div>
    </div>
  `,
  styleUrls: ['./analytics-cards.component.scss']
})
export class AnalyticsCardsComponent implements OnInit {
  private analyticsService = inject(AnalyticsService);

  analyticsCards$!: Observable<AnalyticsCard[]>;
  lastUpdated$!: Observable<Date>;

  ngOnInit(): void {
    this.initializeAnalytics();
    this.setupRealTimeUpdates();
  }

  private initializeAnalytics(): void {
    this.analyticsCards$ = this.analyticsService.getDashboardAnalytics().pipe(
      map(data => this.transformToAnalyticsCards(data))
    );
  }

  private setupRealTimeUpdates(): void {
    // Update every 30 seconds
    this.lastUpdated$ = interval(30000).pipe(
      startWith(0),
      map(() => new Date())
    );

    // Refresh analytics data every 2 minutes
    interval(120000).subscribe(() => {
      this.initializeAnalytics();
    });
  }

  private transformToAnalyticsCards(data: any): AnalyticsCard[] {
    return [
      {
        title: 'Total Revenue',
        value: data.totalRevenue || 0,
        previousValue: data.previousRevenue || 0,
        change: this.calculateChange(data.totalRevenue, data.previousRevenue),
        changeType: this.getChangeType(data.totalRevenue, data.previousRevenue),
        icon: 'attach_money',
        color: 'primary',
        format: 'currency',
        description: 'Total revenue from BNPL transactions this month',
        trend: data.revenueTrend || []
      },
      {
        title: 'Active BNPL Plans',
        value: data.activePlans || 0,
        previousValue: data.previousActivePlans || 0,
        change: this.calculateChange(data.activePlans, data.previousActivePlans),
        changeType: this.getChangeType(data.activePlans, data.previousActivePlans),
        icon: 'credit_card',
        color: 'accent',
        format: 'number',
        description: 'Currently active BNPL payment plans',
        trend: data.plansTrend || []
      },
      {
        title: 'Conversion Rate',
        value: (data.conversionRate || 0) / 100,
        previousValue: (data.previousConversionRate || 0) / 100,
        change: this.calculateChange(data.conversionRate, data.previousConversionRate),
        changeType: this.getChangeType(data.conversionRate, data.previousConversionRate),
        icon: 'trending_up',
        color: 'success',
        format: 'percent',
        description: 'BNPL checkout conversion rate',
        trend: data.conversionTrend || []
      },
      {
        title: 'Default Rate',
        value: (data.defaultRate || 0) / 100,
        previousValue: (data.previousDefaultRate || 0) / 100,
        change: this.calculateChange(data.defaultRate, data.previousDefaultRate),
        changeType: this.getChangeType(data.previousDefaultRate, data.defaultRate), // Inverted for default rate
        icon: 'warning',
        color: 'warn',
        format: 'percent',
        description: 'Payment default rate for BNPL plans',
        trend: data.defaultTrend || []
      },
      {
        title: 'Average Order Value',
        value: data.averageOrderValue || 0,
        previousValue: data.previousAverageOrderValue || 0,
        change: this.calculateChange(data.averageOrderValue, data.previousAverageOrderValue),
        changeType: this.getChangeType(data.averageOrderValue, data.previousAverageOrderValue),
        icon: 'shopping_cart',
        color: 'primary',
        format: 'currency',
        description: 'Average value of BNPL transactions',
        trend: data.aovTrend || []
      },
      {
        title: 'Customer Satisfaction',
        value: (data.customerSatisfaction || 0) / 100,
        previousValue: (data.previousCustomerSatisfaction || 0) / 100,
        change: this.calculateChange(data.customerSatisfaction, data.previousCustomerSatisfaction),
        changeType: this.getChangeType(data.customerSatisfaction, data.previousCustomerSatisfaction),
        icon: 'sentiment_satisfied',
        color: 'success',
        format: 'percent',
        description: 'Customer satisfaction score based on feedback',
        trend: data.satisfactionTrend || []
      }
    ];
  }

  private calculateChange(current: number, previous: number): number {
    if (!previous || previous === 0) return 0;
    return (current - previous) / previous;
  }

  private getChangeType(current: number, previous: number): 'increase' | 'decrease' | 'neutral' {
    const change = this.calculateChange(current, previous);
    if (Math.abs(change) < 0.01) return 'neutral';
    return change > 0 ? 'increase' : 'decrease';
  }

  getTrendPoints(trend: number[]): string {
    if (!trend || trend.length === 0) return '';
    
    const maxValue = Math.max(...trend);
    const minValue = Math.min(...trend);
    const range = maxValue - minValue || 1;
    
    return trend
      .map((value, index) => {
        const x = (index / (trend.length - 1)) * 100;
        const y = 30 - ((value - minValue) / range) * 25; // Invert Y axis
        return `${x},${y}`;
      })
      .join(' ');
  }

  getTrendColor(cardColor: string): string {
    const colorMap = {
      'primary': '#1976d2',
      'accent': '#ff4081',
      'warn': '#f44336',
      'success': '#4caf50'
    };
    return colorMap[cardColor as keyof typeof colorMap] || '#1976d2';
  }

  viewDetails(cardTitle: string): void {
    // Navigate to detailed view based on card type
    console.log(`Viewing details for: ${cardTitle}`);
    // This would typically navigate to a detailed analytics page
  }
}