import { Component, OnInit, OnDestroy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { Observable, Subject, interval, takeUntil, startWith, switchMap } from 'rxjs';
import { TransactionService } from '../../../../core/services/transaction.service';
import { WebSocketService } from '../../../../core/services/websocket.service';
import { CurrencyPipe, DatePipe } from '@angular/common';

export interface RealtimeTransaction {
  id: string;
  customerId: string;
  customerName: string;
  amount: number;
  currency: string;
  status: 'pending' | 'completed' | 'failed' | 'processing';
  paymentMethod: string;
  bnplPlan?: string;
  timestamp: Date;
  riskLevel: 'low' | 'medium' | 'high';
  location?: string;
  merchantReference?: string;
}

@Component({
  selector: 'app-real-time-transactions',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatTableModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule,
    MatTooltipModule,
    MatProgressSpinnerModule,
    CurrencyPipe,
    DatePipe
  ],
  template: `
    <mat-card class="realtime-transactions-card">
      <mat-card-header class="card-header">
        <div class="header-content">
          <div class="title-section">
            <mat-card-title class="card-title">
              <mat-icon class="title-icon">timeline</mat-icon>
              Real-time Transactions
            </mat-card-title>
            <mat-card-subtitle class="card-subtitle">
              Live transaction monitoring with Norwegian BNPL processing
            </mat-card-subtitle>
          </div>
          
          <div class="status-indicators">
            <div class="connection-status" [ngClass]="connectionStatus">
              <div class="status-dot"></div>
              <span class="status-text">{{ getConnectionStatusText() }}</span>
            </div>
            
            <div class="transaction-count">
              <mat-icon>receipt</mat-icon>
              <span>{{ (transactions$ | async)?.length || 0 }} active</span>
            </div>
          </div>
        </div>
      </mat-card-header>

      <mat-card-content class="card-content">
        <div class="transactions-container">
          <div *ngIf="isLoading" class="loading-container">
            <mat-spinner diameter="40"></mat-spinner>
            <span class="loading-text">Loading real-time transactions...</span>
          </div>

          <div *ngIf="!isLoading && (transactions$ | async) as transactions" class="transactions-list">
            <div 
              *ngFor="let transaction of transactions; trackBy: trackByTransactionId" 
              class="transaction-item"
              [ngClass]="'status-' + transaction.status"
              [@slideIn]>
              
              <div class="transaction-main">
                <div class="transaction-info">
                  <div class="customer-info">
                    <div class="customer-name">{{ transaction.customerName }}</div>
                    <div class="customer-id">ID: {{ transaction.customerId | slice:0:8 }}...</div>
                  </div>
                  
                  <div class="transaction-details">
                    <div class="amount">
                      {{ transaction.amount | currency:transaction.currency:'symbol':'1.2-2' }}
                    </div>
                    <div class="payment-method">
                      <mat-icon class="method-icon">{{ getPaymentMethodIcon(transaction.paymentMethod) }}</mat-icon>
                      {{ transaction.paymentMethod }}
                    </div>
                  </div>
                </div>

                <div class="transaction-meta">
                  <div class="status-section">
                    <mat-chip 
                      [ngClass]="'chip-' + transaction.status"
                      class="status-chip">
                      <mat-icon class="chip-icon">{{ getStatusIcon(transaction.status) }}</mat-icon>
                      {{ transaction.status | titlecase }}
                    </mat-chip>
                    
                    <mat-chip 
                      [ngClass]="'chip-risk-' + transaction.riskLevel"
                      class="risk-chip"
                      [matTooltip]="'Risk Level: ' + (transaction.riskLevel | titlecase)">
                      <mat-icon class="chip-icon">{{ getRiskIcon(transaction.riskLevel) }}</mat-icon>
                      {{ transaction.riskLevel | titlecase }}
                    </mat-chip>
                  </div>

                  <div class="timestamp">
                    <mat-icon class="time-icon">schedule</mat-icon>
                    {{ transaction.timestamp | date:'HH:mm:ss' }}
                  </div>
                </div>
              </div>

              <div class="transaction-actions">
                <button 
                  mat-icon-button 
                  [matTooltip]="'View transaction details'"
                  (click)="viewTransaction(transaction.id)">
                  <mat-icon>visibility</mat-icon>
                </button>
                
                <button 
                  mat-icon-button 
                  [matTooltip]="'View customer profile'"
                  (click)="viewCustomer(transaction.customerId)">
                  <mat-icon>person</mat-icon>
                </button>
                
                <button 
                  *ngIf="transaction.status === 'pending'"
                  mat-icon-button 
                  color="primary"
                  [matTooltip]="'Process transaction'"
                  (click)="processTransaction(transaction.id)">
                  <mat-icon>play_arrow</mat-icon>
                </button>
              </div>

              <!-- BNPL Plan Info -->
              <div *ngIf="transaction.bnplPlan" class="bnpl-info">
                <mat-icon class="bnpl-icon">credit_card</mat-icon>
                <span class="bnpl-text">{{ transaction.bnplPlan }}</span>
              </div>

              <!-- Location Info -->
              <div *ngIf="transaction.location" class="location-info">
                <mat-icon class="location-icon">location_on</mat-icon>
                <span class="location-text">{{ transaction.location }}</span>
              </div>
            </div>

            <div *ngIf="transactions.length === 0" class="no-transactions">
              <mat-icon class="no-data-icon">inbox</mat-icon>
              <div class="no-data-text">No active transactions</div>
              <div class="no-data-subtitle">New transactions will appear here in real-time</div>
            </div>
          </div>
        </div>
      </mat-card-content>

      <mat-card-actions class="card-actions">
        <button 
          mat-button 
          color="primary" 
          (click)="viewAllTransactions()"
          class="view-all-button">
          <mat-icon>list</mat-icon>
          View All Transactions
        </button>
        
        <button 
          mat-button 
          (click)="refreshTransactions()"
          [disabled]="isLoading"
          class="refresh-button">
          <mat-icon [ngClass]="{'spinning': isLoading}">refresh</mat-icon>
          Refresh
        </button>
      </mat-card-actions>
    </mat-card>
  `,
  styleUrls: ['./real-time-transactions.component.scss'],
  animations: [
    // Add slide-in animation for new transactions
  ]
})
export class RealTimeTransactionsComponent implements OnInit, OnDestroy {
  private transactionService = inject(TransactionService);
  private webSocketService = inject(WebSocketService);
  private destroy$ = new Subject<void>();

  transactions$!: Observable<RealtimeTransaction[]>;
  isLoading = true;
  connectionStatus: 'connected' | 'disconnected' | 'connecting' = 'connecting';

  ngOnInit(): void {
    this.initializeRealTimeTransactions();
    this.setupWebSocketConnection();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    this.webSocketService.disconnect();
  }

  private initializeRealTimeTransactions(): void {
    // Fetch initial transactions and set up polling
    this.transactions$ = interval(5000).pipe(
      startWith(0),
      switchMap(() => this.transactionService.getRealtimeTransactions()),
      takeUntil(this.destroy$)
    );

    // Set loading to false after first load
    this.transactions$.pipe(takeUntil(this.destroy$)).subscribe(() => {
      this.isLoading = false;
    });
  }

  private setupWebSocketConnection(): void {
    // Connect to WebSocket for real-time updates
    this.webSocketService.connect('transactions').pipe(
      takeUntil(this.destroy$)
    ).subscribe({
      next: (status) => {
        this.connectionStatus = status;
      },
      error: () => {
        this.connectionStatus = 'disconnected';
      }
    });

    // Listen for new transactions
    this.webSocketService.onMessage('new-transaction').pipe(
      takeUntil(this.destroy$)
    ).subscribe((transaction: RealtimeTransaction) => {
      // Handle new transaction notification
      this.handleNewTransaction(transaction);
    });

    // Listen for transaction updates
    this.webSocketService.onMessage('transaction-updated').pipe(
      takeUntil(this.destroy$)
    ).subscribe((transaction: RealtimeTransaction) => {
      // Handle transaction status updates
      this.handleTransactionUpdate(transaction);
    });
  }

  private handleNewTransaction(transaction: RealtimeTransaction): void {
    // Show notification or update UI for new transaction
    console.log('New transaction received:', transaction);
  }

  private handleTransactionUpdate(transaction: RealtimeTransaction): void {
    // Handle transaction status updates
    console.log('Transaction updated:', transaction);
  }

  getConnectionStatusText(): string {
    switch (this.connectionStatus) {
      case 'connected': return 'Live';
      case 'disconnected': return 'Offline';
      case 'connecting': return 'Connecting...';
      default: return 'Unknown';
    }
  }

  getPaymentMethodIcon(method: string): string {
    const iconMap: { [key: string]: string } = {
      'vipps': 'phone_android',
      'card': 'credit_card',
      'bank': 'account_balance',
      'bnpl': 'schedule',
      'default': 'payment'
    };
    return iconMap[method.toLowerCase()] || iconMap['default'];
  }

  getStatusIcon(status: string): string {
    const iconMap: { [key: string]: string } = {
      'pending': 'schedule',
      'processing': 'sync',
      'completed': 'check_circle',
      'failed': 'error',
      'default': 'help'
    };
    return iconMap[status] || iconMap['default'];
  }

  getRiskIcon(riskLevel: string): string {
    const iconMap: { [key: string]: string } = {
      'low': 'check_circle',
      'medium': 'warning',
      'high': 'error',
      'default': 'help'
    };
    return iconMap[riskLevel] || iconMap['default'];
  }

  trackByTransactionId(index: number, transaction: RealtimeTransaction): string {
    return transaction.id;
  }

  viewTransaction(transactionId: string): void {
    // Navigate to transaction details
    console.log('Viewing transaction:', transactionId);
  }

  viewCustomer(customerId: string): void {
    // Navigate to customer profile
    console.log('Viewing customer:', customerId);
  }

  processTransaction(transactionId: string): void {
    // Process pending transaction
    console.log('Processing transaction:', transactionId);
  }

  viewAllTransactions(): void {
    // Navigate to transactions page
    console.log('Viewing all transactions');
  }

  refreshTransactions(): void {
    this.isLoading = true;
    this.initializeRealTimeTransactions();
  }
}