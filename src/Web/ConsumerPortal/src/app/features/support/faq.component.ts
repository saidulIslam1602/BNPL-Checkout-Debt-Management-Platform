import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-faq',
  standalone: true,
  imports: [
    CommonModule,
    MatExpansionModule,
    MatIconModule,
    MatButtonModule
  ],
  template: `
    <div class="faq-container">
      <div class="hero-section">
        <h1>Frequently Asked Questions</h1>
        <p class="subtitle">Find answers to common questions about our BNPL service</p>
      </div>

      <div class="faq-section">
        <mat-accordion>
          @for (faq of faqs(); track faq.id) {
            <mat-expansion-panel>
              <mat-expansion-panel-header>
                <mat-panel-title>{{faq.question}}</mat-panel-title>
              </mat-expansion-panel-header>
              <div [innerHTML]="faq.answer"></div>
            </mat-expansion-panel>
          }
        </mat-accordion>
      </div>

      <div class="contact-section">
        <h2>Still have questions?</h2>
        <p>Our customer support team is here to help</p>
        <button mat-raised-button color="primary" routerLink="/contact">
          Contact Support
        </button>
      </div>
    </div>
  `,
  styles: [`
    .faq-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }

    .hero-section {
      text-align: center;
      margin-bottom: 3rem;
      
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

    .faq-section {
      margin-bottom: 3rem;
    }

    .contact-section {
      text-align: center;
      padding: 2rem;
      background: #f5f5f5;
      border-radius: 8px;
      
      h2 {
        margin-bottom: 1rem;
        color: #333;
      }
      
      p {
        margin-bottom: 1.5rem;
        color: #666;
      }
    }
  `]
})
export class FaqComponent {
  faqs = signal([
    {
      id: 1,
      question: 'What is Buy Now, Pay Later (BNPL)?',
      answer: 'BNPL allows you to purchase items immediately and pay for them over time in installments. You can split your purchase into 3, 4, 6, 12, or 24 payments depending on the amount and your creditworthiness.'
    },
    {
      id: 2,
      question: 'How do I qualify for BNPL?',
      answer: 'To qualify, you must be at least 18 years old, have a valid Norwegian bank account, and pass our credit assessment. We use advanced AI to evaluate your application in real-time.'
    },
    {
      id: 3,
      question: 'Are there any fees?',
      answer: 'Short-term plans (3-4 installments) have no interest or fees. Longer plans may have competitive interest rates. All fees are clearly displayed before you confirm your purchase.'
    },
    {
      id: 4,
      question: 'What happens if I miss a payment?',
      answer: 'If you miss a payment, we\'ll send you a reminder. Late fees may apply after a grace period. We work with customers to find solutions and avoid negative credit impacts.'
    },
    {
      id: 5,
      question: 'Can I pay off my balance early?',
      answer: 'Yes! You can pay off your remaining balance at any time without penalty. This may reduce the total interest you pay on longer-term plans.'
    },
    {
      id: 6,
      question: 'Is my personal information secure?',
      answer: 'Absolutely. We use bank-level encryption and security measures to protect your data. We comply with GDPR and Norwegian data protection regulations.'
    },
    {
      id: 7,
      question: 'Which merchants accept BNPL?',
      answer: 'We partner with thousands of merchants across Norway, including major retailers in electronics, fashion, home goods, and more. Check our merchants page for the full list.'
    },
    {
      id: 8,
      question: 'How do refunds work?',
      answer: 'If you return an item, the refund will be applied to your BNPL plan. Depending on your payment schedule, this may reduce future installments or provide a direct refund.'
    }
  ]);
}