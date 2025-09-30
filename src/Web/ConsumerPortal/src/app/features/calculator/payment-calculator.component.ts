import { Component, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSliderModule } from '@angular/material/slider';

@Component({
  selector: 'app-payment-calculator',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSliderModule
  ],
  template: `
    <div class="calculator-container">
      <div class="hero-section">
        <h1>Payment Calculator</h1>
        <p class="subtitle">Calculate your BNPL installments and see what works best for you</p>
      </div>

      <div class="calculator-section">
        <mat-card class="calculator-card">
          <mat-card-header>
            <mat-card-title>Calculate Your Payments</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <div class="input-section">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Purchase Amount (NOK)</mat-label>
                <input matInput type="number" [(ngModel)]="purchaseAmount" (ngModelChange)="onAmountChange()" min="100" max="50000">
                <mat-icon matSuffix>attach_money</mat-icon>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Payment Plan</mat-label>
                <mat-select [(ngModel)]="selectedPlan" (selectionChange)="onPlanChange()">
                  @for (plan of paymentPlans(); track plan.value) {
                    <mat-option [value]="plan.value">
                      {{plan.label}} - {{plan.description}}
                    </mat-option>
                  }
                </mat-select>
              </mat-form-field>
            </div>

            <div class="results-section">
              <h3>Payment Breakdown</h3>
              <div class="breakdown-grid">
                <div class="breakdown-item">
                  <span class="label">Total Amount:</span>
                  <span class="value">{{purchaseAmount() | currency:'NOK':'symbol':'1.2-2'}}</span>
                </div>
                <div class="breakdown-item">
                  <span class="label">Number of Payments:</span>
                  <span class="value">{{selectedPlan()}}</span>
                </div>
                <div class="breakdown-item">
                  <span class="label">Payment Amount:</span>
                  <span class="value highlight">{{installmentAmount() | currency:'NOK':'symbol':'1.2-2'}}</span>
                </div>
                <div class="breakdown-item">
                  <span class="label">Interest Rate:</span>
                  <span class="value">{{interestRate()}}% APR</span>
                </div>
                <div class="breakdown-item">
                  <span class="label">Total Interest:</span>
                  <span class="value">{{totalInterest() | currency:'NOK':'symbol':'1.2-2'}}</span>
                </div>
                <div class="breakdown-item total">
                  <span class="label">Total to Pay:</span>
                  <span class="value">{{totalToPay() | currency:'NOK':'symbol':'1.2-2'}}</span>
                </div>
              </div>
            </div>

            <div class="schedule-section">
              <h3>Payment Schedule</h3>
              <div class="schedule-list">
                @for (payment of paymentSchedule(); track $index) {
                  <div class="schedule-item">
                    <div class="payment-number">Payment {{$index + 1}}</div>
                    <div class="payment-date">{{payment.date | date:'mediumDate'}}</div>
                    <div class="payment-amount">{{payment.amount | currency:'NOK':'symbol':'1.2-2'}}</div>
                  </div>
                }
              </div>
            </div>
          </mat-card-content>
          <mat-card-actions>
            <button mat-raised-button color="primary" (click)="startCheckout()" [disabled]="purchaseAmount() < 100">
              Start Checkout
            </button>
            <button mat-button (click)="reset()">Reset</button>
          </mat-card-actions>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .calculator-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }

    .hero-section {
      text-align: center;
      margin-bottom: 2rem;
      
      h1 {
        font-size: 2.5rem;
        margin-bottom: 1rem;
        color: #1976d2;
      }
      
      .subtitle {
        font-size: 1.1rem;
        color: #666;
      }
    }

    .calculator-card {
      margin-top: 2rem;
    }

    .input-section {
      margin-bottom: 2rem;
    }

    .full-width {
      width: 100%;
      margin-bottom: 1rem;
    }

    .results-section, .schedule-section {
      margin: 2rem 0;
      
      h3 {
        margin-bottom: 1rem;
        color: #333;
      }
    }

    .breakdown-grid {
      display: grid;
      gap: 0.5rem;
    }

    .breakdown-item {
      display: flex;
      justify-content: space-between;
      padding: 0.5rem 0;
      border-bottom: 1px solid #eee;
      
      &.total {
        border-top: 2px solid #1976d2;
        font-weight: bold;
        font-size: 1.1rem;
      }
      
      .highlight {
        color: #1976d2;
        font-weight: bold;
      }
    }

    .schedule-list {
      max-height: 300px;
      overflow-y: auto;
    }

    .schedule-item {
      display: grid;
      grid-template-columns: 1fr 2fr 1fr;
      gap: 1rem;
      padding: 0.75rem;
      border-bottom: 1px solid #eee;
      align-items: center;
      
      .payment-number {
        font-weight: 500;
      }
      
      .payment-date {
        color: #666;
      }
      
      .payment-amount {
        text-align: right;
        font-weight: bold;
        color: #1976d2;
      }
    }
  `]
})
export class PaymentCalculatorComponent implements OnInit {
  purchaseAmount = signal(5000);
  selectedPlan = signal(4);

  paymentPlans = signal([
    { value: 3, label: 'Pay in 3', description: '0% interest' },
    { value: 4, label: 'Pay in 4', description: '0% interest' },
    { value: 6, label: 'Pay in 6', description: '5.9% APR' },
    { value: 12, label: 'Pay in 12', description: '9.9% APR' },
    { value: 24, label: 'Pay in 24', description: '14.9% APR' }
  ]);

  categories = signal([
    {
      id: 1,
      name: 'Electronics',
      icon: 'devices',
      description: 'Phones, laptops, and gadgets',
      tags: ['Apple', 'Samsung', 'Sony']
    },
    {
      id: 2,
      name: 'Fashion',
      icon: 'checkroom',
      description: 'Clothing and accessories',
      tags: ['H&M', 'Zara', 'Nike']
    },
    {
      id: 3,
      name: 'Home',
      icon: 'home',
      description: 'Furniture and home decor',
      tags: ['IKEA', 'JYSK', 'Bohus']
    }
  ]);

  featuredMerchants = signal([
    {
      id: 1,
      name: 'Elkjøp',
      category: 'Electronics',
      description: 'Norway\'s largest electronics retailer',
      logo: 'assets/merchants/elkjop.png',
      rating: 4.5,
      activeOffers: 25
    },
    {
      id: 2,
      name: 'Komplett',
      category: 'Electronics',
      description: 'Online tech and gaming specialist',
      logo: 'assets/merchants/komplett.png',
      rating: 4.7,
      activeOffers: 18
    }
  ]);

  interestRate = computed(() => {
    const plan = this.selectedPlan();
    if (plan <= 4) return 0;
    if (plan <= 6) return 5.9;
    if (plan <= 12) return 9.9;
    return 14.9;
  });

  installmentAmount = computed(() => {
    const amount = this.purchaseAmount();
    const plan = this.selectedPlan();
    const interest = this.totalInterest();
    return (amount + interest) / plan;
  });

  totalInterest = computed(() => {
    const amount = this.purchaseAmount();
    const rate = this.interestRate();
    const plan = this.selectedPlan();
    
    if (rate === 0) return 0;
    
    // Simple interest calculation
    return (amount * rate / 100) * (plan / 12);
  });

  totalToPay = computed(() => {
    return this.purchaseAmount() + this.totalInterest();
  });

  paymentSchedule = computed(() => {
    const installment = this.installmentAmount();
    const plan = this.selectedPlan();
    const schedule = [];
    const today = new Date();

    for (let i = 0; i < plan; i++) {
      const paymentDate = new Date(today);
      paymentDate.setMonth(paymentDate.getMonth() + i + 1);
      
      schedule.push({
        date: paymentDate,
        amount: installment
      });
    }

    return schedule;
  });

  ngOnInit() {
    // Initialize calculator
  }

  onAmountChange() {
    // Recalculation happens automatically via computed signals
  }

  onPlanChange() {
    // Recalculation happens automatically via computed signals
  }

  selectCategory(categoryId: number) {
    console.log('Selected category:', categoryId);
  }

  visitMerchant(merchantId: number) {
    console.log('Visit merchant:', merchantId);
  }

  startCheckout() {
    // TODO: Navigate to checkout with calculated values
    console.log('Start checkout with amount:', this.purchaseAmount());
  }

  reset() {
    this.purchaseAmount.set(5000);
    this.selectedPlan.set(4);
  }
}