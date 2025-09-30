import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-how-it-works',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatIconModule,
    MatButtonModule
  ],
  template: `
    <div class="how-it-works-container">
      <div class="hero-section">
        <h1>How BNPL Works</h1>
        <p class="subtitle">Shop now, pay later with flexible installment plans</p>
      </div>

      <div class="steps-section">
        <div class="steps-grid">
          <mat-card class="step-card">
            <mat-card-header>
              <mat-icon mat-card-avatar>shopping_cart</mat-icon>
              <mat-card-title>1. Shop</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <p>Browse and select items from our partner merchants. Add them to your cart and proceed to checkout.</p>
            </mat-card-content>
          </mat-card>

          <mat-card class="step-card">
            <mat-card-header>
              <mat-icon mat-card-avatar>assessment</mat-icon>
              <mat-card-title>2. Apply</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <p>Complete a quick application. We'll assess your eligibility in real-time using advanced risk analysis.</p>
            </mat-card-content>
          </mat-card>

          <mat-card class="step-card">
            <mat-card-header>
              <mat-icon mat-card-avatar>schedule</mat-icon>
              <mat-card-title>3. Choose Plan</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <p>Select from flexible payment plans: Pay in 3, 4, 6, 12, or 24 installments based on your needs.</p>
            </mat-card-content>
          </mat-card>

          <mat-card class="step-card">
            <mat-card-header>
              <mat-icon mat-card-avatar>payment</mat-icon>
              <mat-card-title>4. Pay Over Time</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <p>Make automatic payments on your schedule. No interest on short-term plans, competitive rates on longer terms.</p>
            </mat-card-content>
          </mat-card>
        </div>
      </div>

      <div class="benefits-section">
        <h2>Why Choose Our BNPL Service?</h2>
        <div class="benefits-grid">
          <div class="benefit">
            <mat-icon>security</mat-icon>
            <h3>Secure & Safe</h3>
            <p>Bank-level security with fraud protection and encrypted transactions.</p>
          </div>
          <div class="benefit">
            <mat-icon>speed</mat-icon>
            <h3>Instant Approval</h3>
            <p>Get approved in seconds with our advanced AI-powered risk assessment.</p>
          </div>
          <div class="benefit">
            <mat-icon>account_balance_wallet</mat-icon>
            <h3>No Hidden Fees</h3>
            <p>Transparent pricing with no hidden charges. Pay only what you see.</p>
          </div>
        </div>
      </div>

      <div class="cta-section">
        <button mat-raised-button color="primary" size="large" routerLink="/checkout">
          Start Shopping Now
        </button>
      </div>
    </div>
  `,
  styles: [`
    .how-it-works-container {
      padding: 2rem;
      max-width: 1200px;
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
        font-size: 1.2rem;
        color: #666;
      }
    }

    .steps-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 2rem;
      margin-bottom: 3rem;
    }

    .step-card {
      text-align: center;
      transition: transform 0.2s;
      
      &:hover {
        transform: translateY(-4px);
      }
      
      mat-icon[mat-card-avatar] {
        font-size: 2rem;
        height: 2rem;
        width: 2rem;
        background: #1976d2;
        color: white;
      }
    }

    .benefits-section {
      margin: 3rem 0;
      text-align: center;
      
      h2 {
        margin-bottom: 2rem;
        color: #333;
      }
    }

    .benefits-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 2rem;
    }

    .benefit {
      padding: 1.5rem;
      
      mat-icon {
        font-size: 3rem;
        height: 3rem;
        width: 3rem;
        color: #1976d2;
        margin-bottom: 1rem;
      }
      
      h3 {
        margin: 1rem 0;
        color: #333;
      }
      
      p {
        color: #666;
        line-height: 1.6;
      }
    }

    .cta-section {
      text-align: center;
      margin-top: 3rem;
      
      button {
        padding: 1rem 2rem;
        font-size: 1.1rem;
      }
    }
  `]
})
export class HowItWorksComponent {}