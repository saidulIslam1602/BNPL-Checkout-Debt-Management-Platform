import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatTableModule } from '@angular/material/table';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSelectModule } from '@angular/material/select';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatInputModule } from '@angular/material/input';
import { RouterModule } from '@angular/router';

import { Observable, of } from 'rxjs';
import { Chart, ChartConfiguration, ChartType, registerables } from 'chart.js';
import { BaseChartDirective } from 'ng2-charts';

import { DashboardService } from './dashboard.service';
import { TransactionService } from '../transactions/transaction.service';
import { AnalyticsService } from '../analytics/analytics.service';

Chart.register(...registerables);

interface DashboardMetrics {
  totalRevenue: number;
  totalTransactions: number;
  averageOrderValue: number;
  bnplConversionRate: number;
  pendingSettlements: number;
  riskScore: number;
  monthlyGrowth: number;
  customerSatisfaction: number;
}

interface RecentTransaction {
  id: string;
  customerName: string;
  amount: number;
  status: string;
  paymentMethod: string;
  createdAt: Date;
}

interface TopCustomer {
  name: string;
  email: string;
  totalSpent: number;
  transactionCount: number;
  riskLevel: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule,
    MatProgressBarModule,
    MatChipsModule,
    MatTableModule,
    MatPaginatorModule,
    MatSortModule,
    MatTabsModule,
    MatSelectModule,
    MatFormFieldModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatInputModule,
    BaseChartDirective
  ],
  template: `
    <div class="dashboard-container">
      <!-- Header Section -->
      <div class="dashboard-header">
        <div class="header-content">
          <h1>Dashboard</h1>
          <p class="subtitle">Real-time insights for your Norwegian BNPL business</p>
        </div>
        <div class="header-actions">
          <mat-form-field appearance="outline" class="date-picker">
            <mat-label>Date Range</mat-label>
            <mat-select [(value)]="selectedPeriod" (selectionChange)="onPeriodChange()">
              <mat-option value="today">Today</mat-option>
              <mat-option value="week">This Week</mat-option>
              <mat-option value="month">This Month</mat-option>
              <mat-option value="quarter">This Quarter</mat-option>
              <mat-option value="year">This Year</mat-option>
            </mat-select>
          </mat-form-field>
        </div>
      </div>

      <!-- Key Metrics Cards -->
      <div class="metrics-grid">
        <mat-card class="metric-card revenue-card">
          <mat-card-content>
            <div class="metric-header">
              <mat-icon class="metric-icon">account_balance_wallet</mat-icon>
              <span class="metric-change positive">+{{ metrics.monthlyGrowth }}%</span>
            </div>
            <div class="metric-value">{{ metrics.totalRevenue | currency:'NOK':'symbol':'1.0-0' }}</div>
            <div class="metric-label">Total Revenue</div>
            <div class="metric-subtitle">{{ getCurrentPeriodText() }}</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metric-card transactions-card">
          <mat-card-content>
            <div class="metric-header">
              <mat-icon class="metric-icon">payment</mat-icon>
              <span class="metric-change positive">+12.5%</span>
            </div>
            <div class="metric-value">{{ metrics.totalTransactions | number }}</div>
            <div class="metric-label">Total Transactions</div>
            <div class="metric-subtitle">{{ getCurrentPeriodText() }}</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metric-card aov-card">
          <mat-card-content>
            <div class="metric-header">
              <mat-icon class="metric-icon">shopping_cart</mat-icon>
              <span class="metric-change positive">+8.3%</span>
            </div>
            <div class="metric-value">{{ metrics.averageOrderValue | currency:'NOK':'symbol':'1.0-0' }}</div>
            <div class="metric-label">Average Order Value</div>
            <div class="metric-subtitle">Norwegian market avg: 2,450 NOK</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metric-card conversion-card">
          <mat-card-content>
            <div class="metric-header">
              <mat-icon class="metric-icon">trending_up</mat-icon>
              <span class="metric-change positive">+5.7%</span>
            </div>
            <div class="metric-value">{{ metrics.bnplConversionRate }}%</div>
            <div class="metric-label">BNPL Conversion Rate</div>
            <div class="metric-subtitle">Industry avg: 18.2%</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metric-card settlements-card">
          <mat-card-content>
            <div class="metric-header">
              <mat-icon class="metric-icon">account_balance</mat-icon>
              <span class="metric-status pending">{{ metrics.pendingSettlements }} pending</span>
            </div>
            <div class="metric-value">{{ pendingSettlementAmount | currency:'NOK':'symbol':'1.0-0' }}</div>
            <div class="metric-label">Pending Settlements</div>
            <div class="metric-subtitle">Next payout: {{ nextPayoutDate | date:'shortDate' }}</div>
          </mat-card-content>
        </mat-card>

        <mat-card class="metric-card risk-card">
          <mat-card-content>
            <div class="metric-header">
              <mat-icon class="metric-icon">security</mat-icon>
              <div class="risk-indicator" [class]="getRiskClass()">{{ getRiskLevel() }}</div>
            </div>
            <div class="metric-value">{{ metrics.riskScore }}/100</div>
            <div class="metric-label">Portfolio Risk Score</div>
            <div class="metric-subtitle">Norwegian credit standards</div>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Charts Section -->
      <div class="charts-section">
        <div class="charts-grid">
          <!-- Revenue Chart -->
          <mat-card class="chart-card">
            <mat-card-header>
              <mat-card-title>Revenue Trend</mat-card-title>
              <mat-card-subtitle>Daily revenue over the selected period</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <div class="chart-container">
                <canvas baseChart
                        [data]="revenueChartData"
                        [options]="revenueChartOptions"
                        [type]="'line'">
                </canvas>
              </div>
            </mat-card-content>
          </mat-card>

          <!-- Payment Methods Chart -->
          <mat-card class="chart-card">
            <mat-card-header>
              <mat-card-title>Payment Methods</mat-card-title>
              <mat-card-subtitle>Distribution by payment type</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <div class="chart-container">
                <canvas baseChart
                        [data]="paymentMethodsChartData"
                        [options]="paymentMethodsChartOptions"
                        [type]="'doughnut'">
                </canvas>
              </div>
            </mat-card-content>
          </mat-card>
        </div>

        <!-- BNPL Performance Chart -->
        <mat-card class="chart-card full-width">
          <mat-card-header>
            <mat-card-title>BNPL Plan Performance</mat-card-title>
            <mat-card-subtitle>Conversion rates and volumes by plan type</mat-card-subtitle>
          </mat-card-header>
          <mat-card-content>
            <div class="chart-container">
              <canvas baseChart
                      [data]="bnplPerformanceChartData"
                      [options]="bnplPerformanceChartOptions"
                      [type]="'bar'">
              </canvas>
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <!-- Tables Section -->
      <div class="tables-section">
        <div class="tables-grid">
          <!-- Recent Transactions -->
          <mat-card class="table-card">
            <mat-card-header>
              <mat-card-title>Recent Transactions</mat-card-title>
              <mat-card-subtitle>Latest customer payments</mat-card-subtitle>
              <div class="card-actions">
                <button mat-button routerLink="/transactions">View All</button>
              </div>
            </mat-card-header>
            <mat-card-content>
              <mat-table [dataSource]="recentTransactions" class="transactions-table">
                <ng-container matColumnDef="customer">
                  <mat-header-cell *matHeaderCellDef>Customer</mat-header-cell>
                  <mat-cell *matCellDef="let transaction">{{ transaction.customerName }}</mat-cell>
                </ng-container>

                <ng-container matColumnDef="amount">
                  <mat-header-cell *matHeaderCellDef>Amount</mat-header-cell>
                  <mat-cell *matCellDef="let transaction">
                    {{ transaction.amount | currency:'NOK':'symbol':'1.0-0' }}
                  </mat-cell>
                </ng-container>

                <ng-container matColumnDef="method">
                  <mat-header-cell *matHeaderCellDef>Method</mat-header-cell>
                  <mat-cell *matCellDef="let transaction">
                    <mat-chip class="payment-method-chip" [class]="getPaymentMethodClass(transaction.paymentMethod)">
                      {{ transaction.paymentMethod }}
                    </mat-chip>
                  </mat-cell>
                </ng-container>

                <ng-container matColumnDef="status">
                  <mat-header-cell *matHeaderCellDef>Status</mat-header-cell>
                  <mat-cell *matCellDef="let transaction">
                    <mat-chip class="status-chip" [class]="getStatusClass(transaction.status)">
                      {{ transaction.status }}
                    </mat-chip>
                  </mat-cell>
                </ng-container>

                <ng-container matColumnDef="date">
                  <mat-header-cell *matHeaderCellDef>Date</mat-header-cell>
                  <mat-cell *matCellDef="let transaction">
                    {{ transaction.createdAt | date:'short' }}
                  </mat-cell>
                </ng-container>

                <mat-header-row *matHeaderRowDef="transactionColumns"></mat-header-row>
                <mat-row *matRowDef="let row; columns: transactionColumns;"></mat-row>
              </mat-table>
            </mat-card-content>
          </mat-card>

          <!-- Top Customers -->
          <mat-card class="table-card">
            <mat-card-header>
              <mat-card-title>Top Customers</mat-card-title>
              <mat-card-subtitle>Highest value customers this period</mat-card-subtitle>
              <div class="card-actions">
                <button mat-button routerLink="/customers">View All</button>
              </div>
            </mat-card-header>
            <mat-card-content>
              <mat-table [dataSource]="topCustomers" class="customers-table">
                <ng-container matColumnDef="name">
                  <mat-header-cell *matHeaderCellDef>Customer</mat-header-cell>
                  <mat-cell *matCellDef="let customer">
                    <div class="customer-info">
                      <div class="customer-name">{{ customer.name }}</div>
                      <div class="customer-email">{{ customer.email }}</div>
                    </div>
                  </mat-cell>
                </ng-container>

                <ng-container matColumnDef="spent">
                  <mat-header-cell *matHeaderCellDef>Total Spent</mat-header-cell>
                  <mat-cell *matCellDef="let customer">
                    {{ customer.totalSpent | currency:'NOK':'symbol':'1.0-0' }}
                  </mat-cell>
                </ng-container>

                <ng-container matColumnDef="transactions">
                  <mat-header-cell *matHeaderCellDef>Orders</mat-header-cell>
                  <mat-cell *matCellDef="let customer">{{ customer.transactionCount }}</mat-cell>
                </ng-container>

                <ng-container matColumnDef="risk">
                  <mat-header-cell *matHeaderCellDef>Risk</mat-header-cell>
                  <mat-cell *matCellDef="let customer">
                    <mat-chip class="risk-chip" [class]="getRiskChipClass(customer.riskLevel)">
                      {{ customer.riskLevel }}
                    </mat-chip>
                  </mat-cell>
                </ng-container>

                <mat-header-row *matHeaderRowDef="customerColumns"></mat-header-row>
                <mat-row *matRowDef="let row; columns: customerColumns;"></mat-row>
              </mat-table>
            </mat-card-content>
          </mat-card>
        </div>
      </div>

      <!-- Norwegian Market Insights -->
      <mat-card class="insights-card">
        <mat-card-header>
          <mat-card-title>
            <mat-icon>insights</mat-icon>
            Norwegian Market Insights
          </mat-card-title>
          <mat-card-subtitle>Real-time market intelligence and trends</mat-card-subtitle>
        </mat-card-header>
        <mat-card-content>
          <div class="insights-grid">
            <div class="insight-item">
              <mat-icon class="insight-icon">trending_up</mat-icon>
              <div class="insight-content">
                <div class="insight-title">BNPL Growth</div>
                <div class="insight-description">Norwegian BNPL market grew 34% YoY, reaching 12.8B NOK in 2024</div>
              </div>
            </div>
            <div class="insight-item">
              <mat-icon class="insight-icon">people</mat-icon>
              <div class="insight-content">
                <div class="insight-title">Consumer Adoption</div>
                <div class="insight-description">68% of Norwegian millennials have used BNPL services in the past year</div>
              </div>
            </div>
            <div class="insight-item">
              <mat-icon class="insight-icon">security</mat-icon>
              <div class="insight-content">
                <div class="insight-title">Credit Quality</div>
                <div class="insight-description">Norwegian BNPL default rates remain low at 2.1%, below EU average</div>
              </div>
            </div>
            <div class="insight-item">
              <mat-icon class="insight-icon">phone_android</mat-icon>
              <div class="insight-content">
                <div class="insight-title">Mobile Commerce</div>
                <div class="insight-description">78% of Norwegian BNPL transactions are completed on mobile devices</div>
              </div>
            </div>
          </div>
        </mat-card-content>
      </mat-card>
    </div>
  `,
  styleUrls: ['./dashboard.component.scss']
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  private transactionService = inject(TransactionService);
  private analyticsService = inject(AnalyticsService);

  selectedPeriod = 'month';
  metrics: DashboardMetrics = {
    totalRevenue: 2847500,
    totalTransactions: 1247,
    averageOrderValue: 2284,
    bnplConversionRate: 23.8,
    pendingSettlements: 8,
    riskScore: 87,
    monthlyGrowth: 15.7,
    customerSatisfaction: 4.6
  };

  pendingSettlementAmount = 456780;
  nextPayoutDate = new Date(Date.now() + 2 * 24 * 60 * 60 * 1000); // 2 days from now

  recentTransactions: RecentTransaction[] = [];
  topCustomers: TopCustomer[] = [];

  transactionColumns = ['customer', 'amount', 'method', 'status', 'date'];
  customerColumns = ['name', 'spent', 'transactions', 'risk'];

  // Chart configurations
  revenueChartData: ChartConfiguration['data'] = {
    labels: [],
    datasets: []
  };

  revenueChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: false
      }
    },
    scales: {
      y: {
        beginAtZero: true,
        ticks: {
          callback: function(value) {
            return new Intl.NumberFormat('no-NO', {
              style: 'currency',
              currency: 'NOK',
              minimumFractionDigits: 0
            }).format(value as number);
          }
        }
      }
    }
  };

  paymentMethodsChartData: ChartConfiguration['data'] = {
    labels: [],
    datasets: []
  };

  paymentMethodsChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        position: 'bottom'
      }
    }
  };

  bnplPerformanceChartData: ChartConfiguration['data'] = {
    labels: [],
    datasets: []
  };

  bnplPerformanceChartOptions: ChartConfiguration['options'] = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: true
      }
    },
    scales: {
      y: {
        beginAtZero: true
      }
    }
  };

  ngOnInit(): void {
    this.loadDashboardData();
  }

  onPeriodChange(): void {
    this.loadDashboardData();
  }

  getCurrentPeriodText(): string {
    const periodMap: { [key: string]: string } = {
      'today': 'Today',
      'week': 'This week',
      'month': 'This month',
      'quarter': 'This quarter',
      'year': 'This year'
    };
    return periodMap[this.selectedPeriod] || 'This month';
  }

  getRiskLevel(): string {
    if (this.metrics.riskScore >= 80) return 'LOW';
    if (this.metrics.riskScore >= 60) return 'MEDIUM';
    if (this.metrics.riskScore >= 40) return 'HIGH';
    return 'VERY HIGH';
  }

  getRiskClass(): string {
    const level = this.getRiskLevel();
    return `risk-${level.toLowerCase().replace(' ', '-')}`;
  }

  getPaymentMethodClass(method: string): string {
    return `method-${method.toLowerCase().replace('_', '-')}`;
  }

  getStatusClass(status: string): string {
    return `status-${status.toLowerCase()}`;
  }

  getRiskChipClass(risk: string): string {
    return `risk-${risk.toLowerCase()}`;
  }

  private loadDashboardData(): void {
    // Load recent transactions
    this.recentTransactions = [
      {
        id: '1',
        customerName: 'Lars Andersen',
        amount: 3450,
        status: 'COMPLETED',
        paymentMethod: 'BNPL',
        createdAt: new Date(Date.now() - 2 * 60 * 60 * 1000)
      },
      {
        id: '2',
        customerName: 'Ingrid Olsen',
        amount: 1890,
        status: 'PROCESSING',
        paymentMethod: 'VIPPS',
        createdAt: new Date(Date.now() - 4 * 60 * 60 * 1000)
      },
      {
        id: '3',
        customerName: 'Erik Johansen',
        amount: 5670,
        status: 'COMPLETED',
        paymentMethod: 'CREDIT_CARD',
        createdAt: new Date(Date.now() - 6 * 60 * 60 * 1000)
      },
      {
        id: '4',
        customerName: 'Astrid Hansen',
        amount: 2340,
        status: 'COMPLETED',
        paymentMethod: 'BNPL',
        createdAt: new Date(Date.now() - 8 * 60 * 60 * 1000)
      },
      {
        id: '5',
        customerName: 'Magnus Berg',
        amount: 4120,
        status: 'FAILED',
        paymentMethod: 'BANK_TRANSFER',
        createdAt: new Date(Date.now() - 10 * 60 * 60 * 1000)
      }
    ];

    // Load top customers
    this.topCustomers = [
      {
        name: 'Nora Kristiansen',
        email: 'nora.k@example.no',
        totalSpent: 45670,
        transactionCount: 23,
        riskLevel: 'LOW'
      },
      {
        name: 'Henrik Larsen',
        email: 'henrik.l@example.no',
        totalSpent: 38920,
        transactionCount: 18,
        riskLevel: 'LOW'
      },
      {
        name: 'Emma Svendsen',
        email: 'emma.s@example.no',
        totalSpent: 32450,
        transactionCount: 15,
        riskLevel: 'MEDIUM'
      },
      {
        name: 'Oliver Pedersen',
        email: 'oliver.p@example.no',
        totalSpent: 28760,
        transactionCount: 12,
        riskLevel: 'LOW'
      },
      {
        name: 'Sofie Nilsen',
        email: 'sofie.n@example.no',
        totalSpent: 24580,
        transactionCount: 11,
        riskLevel: 'MEDIUM'
      }
    ];

    // Load chart data
    this.loadChartData();
  }

  private loadChartData(): void {
    // Revenue chart data
    this.revenueChartData = {
      labels: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
      datasets: [{
        label: 'Revenue (NOK)',
        data: [125000, 142000, 138000, 156000, 167000, 189000, 178000],
        borderColor: '#1e3a8a',
        backgroundColor: 'rgba(30, 58, 138, 0.1)',
        fill: true,
        tension: 0.4
      }]
    };

    // Payment methods chart data
    this.paymentMethodsChartData = {
      labels: ['BNPL', 'Vipps', 'Credit Card', 'Bank Transfer', 'Debit Card'],
      datasets: [{
        data: [38, 28, 18, 10, 6],
        backgroundColor: [
          '#1e3a8a',
          '#7c3aed',
          '#059669',
          '#dc2626',
          '#ea580c'
        ],
        borderWidth: 0
      }]
    };

    // BNPL performance chart data
    this.bnplPerformanceChartData = {
      labels: ['Pay in 3', 'Pay in 4', 'Pay in 6', 'Pay in 12', 'Pay in 24'],
      datasets: [
        {
          label: 'Conversion Rate (%)',
          data: [28.5, 24.8, 18.2, 15.6, 8.9],
          backgroundColor: '#1e3a8a',
          yAxisID: 'y'
        },
        {
          label: 'Volume (NOK)',
          data: [850000, 720000, 540000, 420000, 180000],
          backgroundColor: '#059669',
          yAxisID: 'y1'
        }
      ]
    };

    this.bnplPerformanceChartOptions = {
      responsive: true,
      maintainAspectRatio: false,
      plugins: {
        legend: {
          display: true
        }
      },
      scales: {
        y: {
          type: 'linear',
          display: true,
          position: 'left',
          title: {
            display: true,
            text: 'Conversion Rate (%)'
          }
        },
        y1: {
          type: 'linear',
          display: true,
          position: 'right',
          title: {
            display: true,
            text: 'Volume (NOK)'
          },
          grid: {
            drawOnChartArea: false,
          },
          ticks: {
            callback: function(value) {
              return new Intl.NumberFormat('no-NO', {
                style: 'currency',
                currency: 'NOK',
                minimumFractionDigits: 0
              }).format(value as number);
            }
          }
        }
      }
    };
  }
}