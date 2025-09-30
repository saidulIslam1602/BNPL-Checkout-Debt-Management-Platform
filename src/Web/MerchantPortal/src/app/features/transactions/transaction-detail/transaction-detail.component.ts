import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';

@Component({
  selector: 'app-transaction-detail',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule
  ],
  template: `
    <div class="transaction-detail-container">
      <div class="header-section">
        <h1>Transaction Details</h1>
        <mat-chip [color]="getStatusColor(transaction().status)">
          {{transaction().status | titlecase}}
        </mat-chip>
      </div>

      <div class="details-grid">
        <mat-card>
          <mat-card-header>
            <mat-card-title>Transaction Information</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="detail-row">
              <span class="label">Order ID:</span>
              <span class="value">{{transaction().orderId}}</span>
            </div>
            <div class="detail-row">
              <span class="label">Amount:</span>
              <span class="value">{{transaction().amount | currency:'NOK':'symbol':'1.2-2'}}</span>
            </div>
            <div class="detail-row">
              <span class="label">Customer:</span>
              <span class="value">{{transaction().customerName}}</span>
            </div>
            <div class="detail-row">
              <span class="label">Date:</span>
              <span class="value">{{transaction().createdAt | date:'medium'}}</span>
            </div>
          </mat-card-content>
        </mat-card>

        <mat-card>
          <mat-card-header>
            <mat-card-title>Payment Plan</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="detail-row">
              <span class="label">Plan Type:</span>
              <span class="value">{{transaction().planType}}</span>
            </div>
            <div class="detail-row">
              <span class="label">Installments:</span>
              <span class="value">{{transaction().paidInstallments}}/{{transaction().totalInstallments}}</span>
            </div>
            <div class="detail-row">
              <span class="label">Next Payment:</span>
              <span class="value">{{transaction().nextPaymentDate | date:'mediumDate'}}</span>
            </div>
          </mat-card-content>
        </mat-card>
      </div>

      <div class="actions-section">
        <button mat-raised-button color="primary" (click)="processRefund()">
          Process Refund
        </button>
        <button mat-button (click)="goBack()">
          Back to Transactions
        </button>
      </div>
    </div>
  `,
  styles: [`
    .transaction-detail-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }

    .header-section {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 2rem;
      
      h1 {
        font-size: 2rem;
        color: #1976d2;
        margin: 0;
      }
    }

    .details-grid {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 2rem;
      margin-bottom: 2rem;
      
      @media (max-width: 768px) {
        grid-template-columns: 1fr;
      }
    }

    .detail-row {
      display: flex;
      justify-content: space-between;
      padding: 0.5rem 0;
      border-bottom: 1px solid #eee;
      
      .label {
        font-weight: 500;
        color: #666;
      }
      
      .value {
        color: #333;
      }
    }

    .actions-section {
      display: flex;
      gap: 1rem;
      justify-content: center;
    }
  `]
})
export class TransactionDetailComponent implements OnInit {
  transactionId = signal('');
  
  transaction = signal({
    id: '1',
    orderId: 'ORD-12345678',
    customerName: 'John Doe',
    amount: 15000,
    status: 'completed',
    planType: 'Pay in 4',
    totalInstallments: 4,
    paidInstallments: 4,
    nextPaymentDate: new Date(),
    createdAt: new Date(Date.now() - 24 * 60 * 60 * 1000)
  });

  constructor(private route: ActivatedRoute) {}

  ngOnInit() {
    this.route.params.subscribe(params => {
      this.transactionId.set(params['id']);
      this.loadTransaction();
    });
  }

  loadTransaction() {
    // TODO: Load transaction from API
  }

  getStatusColor(status: string): 'primary' | 'accent' | 'warn' {
    switch (status.toLowerCase()) {
      case 'completed': return 'primary';
      case 'pending': return 'accent';
      case 'failed': return 'warn';
      default: return 'primary';
    }
  }

  processRefund() {
    console.log('Processing refund for transaction:', this.transactionId());
  }

  goBack() {
    window.history.back();
  }
}