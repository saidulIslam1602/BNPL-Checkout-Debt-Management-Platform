import { Component, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatChipsModule } from '@angular/material/chips';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { PaymentService } from '../../core/services/payment.service';
import { Payment } from '../../core/models/payment.model';

@Component({
  selector: 'app-my-payments',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatChipsModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="my-payments-container">
      <h1>My Payments</h1>
      
      @if (loading()) {
        <div class="loading-container">
          <mat-spinner></mat-spinner>
          <p>Loading your payments...</p>
        </div>
      } @else if (payments().length === 0) {
        <div class="empty-state">
          <mat-icon>payment</mat-icon>
          <h2>No payments found</h2>
          <p>You don't have any BNPL payments yet.</p>
          <button mat-raised-button color="primary" routerLink="/checkout">
            Start Shopping
          </button>
        </div>
      } @else {
        <div class="payments-grid">
          @for (payment of payments(); track payment.id) {
            <mat-card class="payment-card">
              <mat-card-header>
                <mat-card-title>Payment #{{payment.orderNumber}}</mat-card-title>
                <mat-card-subtitle>{{payment.createdAt | date}}</mat-card-subtitle>
              </mat-card-header>
              <mat-card-content>
                <div class="payment-info">
                  <div class="amount">{{payment.amount | currency:'NOK':'symbol':'1.2-2'}}</div>
                  <mat-chip [color]="getStatusColor(payment.status)">
                    {{payment.status | titlecase}}
                  </mat-chip>
                </div>
                <div class="installments">
                  <p><strong>Installments:</strong> {{payment.paidInstallments}}/{{payment.totalInstallments}}</p>
                  <p><strong>Next Payment:</strong> {{payment.nextPaymentDate | date}}</p>
                </div>
              </mat-card-content>
              <mat-card-actions>
                <button mat-button (click)="viewPaymentDetails(payment.id)">
                  View Details
                </button>
                @if (payment.status === 'pending') {
                  <button mat-raised-button color="primary" (click)="makePayment(payment.id)">
                    Pay Now
                  </button>
                }
              </mat-card-actions>
            </mat-card>
          }
        </div>
      }
    </div>
  `,
  styles: [`
    .my-payments-container {
      padding: 2rem;
      max-width: 1200px;
      margin: 0 auto;
    }

    .loading-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      padding: 3rem;
    }

    .empty-state {
      text-align: center;
      padding: 3rem;
      
      mat-icon {
        font-size: 4rem;
        height: 4rem;
        width: 4rem;
        color: #666;
      }
    }

    .payments-grid {
      display: grid;
      grid-template-columns: repeat(auto-fill, minmax(350px, 1fr));
      gap: 1.5rem;
      margin-top: 2rem;
    }

    .payment-card {
      transition: transform 0.2s;
      
      &:hover {
        transform: translateY(-2px);
      }
    }

    .payment-info {
      display: flex;
      justify-content: space-between;
      align-items: center;
      margin-bottom: 1rem;
    }

    .amount {
      font-size: 1.5rem;
      font-weight: bold;
      color: #2e7d32;
    }

    .installments {
      p {
        margin: 0.5rem 0;
        color: #666;
      }
    }
  `]
})
export class MyPaymentsComponent implements OnInit {
  payments = signal<Payment[]>([]);
  loading = signal(false);

  constructor(private paymentService: PaymentService) {}

  ngOnInit() {
    this.loadPayments();
  }

  private loadPayments() {
    this.loading.set(true);
    this.paymentService.getMyPayments().subscribe({
      next: (payments) => {
        this.payments.set(payments);
        this.loading.set(false);
      },
      error: (error) => {
        console.error('Failed to load payments:', error);
        this.loading.set(false);
      }
    });
  }

  getStatusColor(status: string): 'primary' | 'accent' | 'warn' {
    switch (status.toLowerCase()) {
      case 'completed': return 'primary';
      case 'pending': return 'accent';
      case 'failed': return 'warn';
      default: return 'primary';
    }
  }

  viewPaymentDetails(paymentId: string) {
    // TODO: Navigate to payment details
    console.log('View payment details:', paymentId);
  }

  makePayment(paymentId: string) {
    // TODO: Navigate to payment flow
    console.log('Make payment:', paymentId);
  }
}