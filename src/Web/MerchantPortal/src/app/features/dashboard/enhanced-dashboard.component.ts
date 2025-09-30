import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatChipsModule } from '@angular/material/chips';
import { Subject, takeUntil, interval, startWith } from 'rxjs';

// Import our enhanced components
import { AnalyticsCardsComponent } from './components/analytics-cards/analytics-cards.component';
import { RealTimeTransactionsComponent } from './components/real-time-transactions/real-time-transactions.component';

// Import services
import { AnalyticsService } from '../../core/services/analytics.service';
import { TransactionService } from '../../core/services/transaction.service';
import { WebSocketService } from '../../core/services/websocket.service';
import { AuthService } from '../../core/services/auth.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-enhanced-dashboard',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatGridListModule,
    MatProgressSpinnerModule,
    MatTooltipModule,
    MatChipsModule,
    AnalyticsCardsComponent,
    RealTimeTransactionsComponent
  ],
  template: `
    <div class="enhanced-dashboard">
      <!-- Dashboard Header -->
      <div class="dashboard-header">
        <div class="header-content">
          <div class="welcome-section">
            <h1 class="dashboard-title">
              <mat-icon class="title-icon">dashboard</mat-icon>
              Welcome back, {{ (currentUser$ | async)?.firstName }}!
            </h1>
            <p class="dashboard-subtitle">
              Here's what's happening with your Norwegian BNPL business today
            </p>
          </div>
          
          <div class="header-actions">
            <div class="connection-status" [ngClass]="connectionStatus">
              <div class="status-dot"></div>
              <span class="status-text">{{ getConnectionStatusText() }}</span>
            </div>
            
            <button 
              mat-raised-button 
              color="primary" 
              (click)="refreshDashboard()"
              [disabled]="isRefreshing"
              class="refresh-button">
              <mat-icon [ngClass]="{'spinning': isRefreshing}">refresh</mat-icon>
              Refresh
            </button>
          </div>
        </div>
        
        <!-- Quick Stats Bar -->
        <div class="quick-stats-bar">
          <div class="stat-item">
            <mat-icon class="stat-icon">trending_up</mat-icon>
            <div class="stat-content">
              <div class="stat-value">{{ quickStats.todayRevenue | currency:'NOK':'symbol':'1.0-0' }}</div>
              <div class="stat-label">Today's Revenue</div>
            </div>
          </div>
          
          <div class="stat-item">
            <mat-icon class="stat-icon">receipt</mat-icon>
            <div class="stat-content">
              <div class="stat-value">{{ quickStats.todayTransactions }}</div>
              <div class="stat-label">Today's Transactions</div>
            </div>
          </div>
          
          <div class="stat-item">
            <mat-icon class="stat-icon">people</mat-icon>
            <div class="stat-content">
              <div class="stat-value">{{ quickStats.activeCustomers }}</div>
              <div class="stat-label">Active Customers</div>
            </div>
          </div>
          
          <div class="stat-item">
            <mat-icon class="stat-icon">security</mat-icon>
            <div class="stat-content">
              <div class="stat-value">{{ quickStats.riskScore }}%</div>
              <div class="stat-label">Risk Score</div>
            </div>
          </div>
        </div>
      </div>

      <!-- Main Dashboard Content -->
      <div class="dashboard-content">
        <mat-tab-group class="dashboard-tabs" animationDuration="300ms">
          <!-- Overview Tab -->
          <mat-tab label="Overview">
            <ng-template matTabContent>
              <div class="tab-content overview-tab">
                <!-- Analytics Cards -->
                <div class="analytics-section">
                  <app-analytics-cards></app-analytics-cards>
                </div>

                <!-- Charts and Real-time Data -->
                <div class="charts-grid">
                  <div class="chart-row">
                    <!-- Real-time Transactions -->
                    <div class="chart-container large">
                      <app-real-time-transactions></app-real-time-transactions>
                    </div>
                    
                    <!-- Norwegian Market Insights -->
                    <div class="chart-container medium">
                      <mat-card class="market-insights-card">
                        <mat-card-header>
                          <mat-card-title>
                            <img src="assets/images/norway-flag.svg" alt="Norway" class="flag-icon">
                            Norwegian Market Insights
                          </mat-card-title>
                          <mat-card-subtitle>
                            Benchmarks and trends for the Norwegian BNPL market
                          </mat-card-subtitle>
                        </mat-card-header>
                        
                        <mat-card-content>
                          <div class="market-metrics">
                            <div class="metric-item">
                              <div class="metric-label">Market Average Conversion</div>
                              <div class="metric-value">65.8%</div>
                              <div class="metric-comparison positive">+2.7% vs your rate</div>
                            </div>
                            
                            <div class="metric-item">
                              <div class="metric-label">Market Average Default Rate</div>
                              <div class="metric-value">3.2%</div>
                              <div class="metric-comparison positive">-0.4% vs your rate</div>
                            </div>
                            
                            <div class="metric-item">
                              <div class="metric-label">Popular Payment Methods</div>
                              <div class="payment-methods">
                                <mat-chip class="method-chip">Vipps (42%)</mat-chip>
                                <mat-chip class="method-chip">Card (31%)</mat-chip>
                                <mat-chip class="method-chip">BNPL (27%)</mat-chip>
                              </div>
                            </div>
                          </div>
                        </mat-card-content>
                      </mat-card>
                    </div>
                  </div>
                </div>
              </div>
            </ng-template>
          </mat-tab>

          <!-- Analytics Tab -->
          <mat-tab label="Analytics">
            <ng-template matTabContent>
              <div class="tab-content analytics-tab">
                <div class="analytics-placeholder">
                  <mat-icon class="placeholder-icon">analytics</mat-icon>
                  <h3>Advanced Analytics</h3>
                  <p>Detailed analytics dashboard coming soon...</p>
                </div>
              </div>
            </ng-template>
          </mat-tab>

          <!-- Risk Management Tab -->
          <mat-tab label="Risk Management">
            <ng-template matTabContent>
              <div class="tab-content risk-tab">
                <div class="risk-placeholder">
                  <mat-icon class="placeholder-icon">security</mat-icon>
                  <h3>Risk Management Dashboard</h3>
                  <p>Real-time risk monitoring and fraud detection dashboard coming soon...</p>
                </div>
              </div>
            </ng-template>
          </mat-tab>

          <!-- Notifications Tab -->
          <mat-tab label="Notifications ({{unreadNotifications}})">
            <ng-template matTabContent>
              <div class="tab-content notifications-tab">
                <div class="notifications-placeholder">
                  <mat-icon class="placeholder-icon">notifications</mat-icon>
                  <h3>Notifications Center</h3>
                  <p>Centralized notification management coming soon...</p>
                </div>
              </div>
            </ng-template>
          </mat-tab>
        </mat-tab-group>
      </div>

      <!-- Loading Overlay -->
      <div *ngIf="isLoading" class="loading-overlay">
        <mat-spinner diameter="50"></mat-spinner>
        <div class="loading-text">Loading dashboard data...</div>
      </div>
    </div>
  `,
  styleUrls: ['./enhanced-dashboard.component.scss']
})
export class EnhancedDashboardComponent implements OnInit, OnDestroy {
  private destroy$ = new Subject<void>();
  
  // Inject services
  private analyticsService = inject(AnalyticsService);
  private transactionService = inject(TransactionService);
  private webSocketService = inject(WebSocketService);
  private authService = inject(AuthService);
  private notificationService = inject(NotificationService);

  // Component state
  isLoading = false;
  isRefreshing = false;
  connectionStatus: 'connected' | 'disconnected' | 'connecting' = 'connecting';
  unreadNotifications = 0;

  // Observables
  currentUser$ = this.authService.currentUser$;

  // Quick stats data
  quickStats = {
    todayRevenue: 125000,
    todayTransactions: 89,
    activeCustomers: 1247,
    riskScore: 92
  };

  ngOnInit(): void {
    this.initializeDashboard();
    this.setupRealTimeUpdates();
    this.setupNotifications();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.webSocketService.disconnect();
  }

  private initializeDashboard(): void {
    this.isLoading = true;

    // Load initial dashboard data
    Promise.all([
      this.loadQuickStats(),
      this.connectWebSocket()
    ]).finally(() => {
      this.isLoading = false;
    });
  }

  private async loadQuickStats(): Promise<void> {
    try {
      // In a real app, this would fetch from the analytics service
      // For now, we'll use mock data
      this.quickStats = {
        todayRevenue: Math.floor(Math.random() * 200000) + 100000,
        todayTransactions: Math.floor(Math.random() * 150) + 50,
        activeCustomers: Math.floor(Math.random() * 500) + 1000,
        riskScore: Math.floor(Math.random() * 20) + 80
      };
    } catch (error) {
      console.error('Error loading quick stats:', error);
      this.notificationService.showError('Failed to load dashboard statistics');
    }
  }

  private async connectWebSocket(): Promise<void> {
    try {
      this.webSocketService.connect('dashboard').pipe(
        takeUntil(this.destroy$)
      ).subscribe(status => {
        this.connectionStatus = status;
        
        if (status === 'connected') {
          this.notificationService.showSuccess('Real-time updates connected');
          this.subscribeToRealTimeEvents();
        } else if (status === 'disconnected') {
          this.notificationService.showWarning('Real-time updates disconnected');
        }
      });
    } catch (error) {
      console.error('Error connecting WebSocket:', error);
      this.connectionStatus = 'disconnected';
    }
  }

  private setupRealTimeUpdates(): void {
    // Update quick stats every 30 seconds
    interval(30000).pipe(
      startWith(0),
      takeUntil(this.destroy$)
    ).subscribe(() => {
      this.updateQuickStats();
    });
  }

  private setupNotifications(): void {
    this.notificationService.unreadCount$.pipe(
      takeUntil(this.destroy$)
    ).subscribe(count => {
      this.unreadNotifications = count;
    });
  }

  private subscribeToRealTimeEvents(): void {
    // Subscribe to new transactions
    this.webSocketService.subscribeToNewTransactions().pipe(
      takeUntil(this.destroy$)
    ).subscribe(transaction => {
      this.handleNewTransaction(transaction);
    });

    // Subscribe to risk alerts
    this.webSocketService.subscribeToRiskAlerts().pipe(
      takeUntil(this.destroy$)
    ).subscribe(alert => {
      this.handleRiskAlert(alert);
    });

    // Subscribe to fraud alerts
    this.webSocketService.subscribeToFraudAlerts().pipe(
      takeUntil(this.destroy$)
    ).subscribe(alert => {
      this.handleFraudAlert(alert);
    });
  }

  private handleNewTransaction(transaction: any): void {
    // Update quick stats
    this.quickStats.todayTransactions++;
    this.quickStats.todayRevenue += transaction.amount;

    // Show notification for high-value transactions
    if (transaction.amount > 10000) {
      this.notificationService.showInfo(
        `High-value transaction: ${transaction.amount} NOK`,
        'New Transaction'
      );
    }
  }

  private handleRiskAlert(alert: any): void {
    this.notificationService.showWarning(
      alert.message,
      'Risk Alert',
      8000
    );
  }

  private handleFraudAlert(alert: any): void {
    this.notificationService.showError(
      alert.message,
      'Fraud Alert',
      10000
    );
  }

  private updateQuickStats(): void {
    // Simulate real-time updates to quick stats
    this.quickStats = {
      ...this.quickStats,
      todayRevenue: this.quickStats.todayRevenue + Math.floor(Math.random() * 5000),
      todayTransactions: this.quickStats.todayTransactions + Math.floor(Math.random() * 3),
      riskScore: Math.max(75, Math.min(100, this.quickStats.riskScore + (Math.random() - 0.5) * 2))
    };
  }

  getConnectionStatusText(): string {
    switch (this.connectionStatus) {
      case 'connected': return 'Live Updates';
      case 'disconnected': return 'Offline';
      case 'connecting': return 'Connecting...';
      default: return 'Unknown';
    }
  }

  refreshDashboard(): void {
    this.isRefreshing = true;
    
    Promise.all([
      this.loadQuickStats(),
      new Promise(resolve => setTimeout(resolve, 1000)) // Simulate API call
    ]).finally(() => {
      this.isRefreshing = false;
      this.notificationService.showSuccess('Dashboard refreshed');
    });
  }
}