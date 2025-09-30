import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatCardModule, MatIconModule],
  template: `
    <div class="home-container">
      <section class="hero-section">
        <div class="hero-content">
          <h1 class="hero-title">Kjøp nå, betal senere</h1>
          <p class="hero-subtitle">Trygg og enkel BNPL-løsning for norske forbrukere</p>
          <div class="hero-actions">
            <button mat-raised-button color="primary" routerLink="/how-it-works" class="cta-button">
              Slik fungerer det
            </button>
            <button mat-button routerLink="/calculator" class="secondary-button">
              <mat-icon>calculate</mat-icon>
              Betalingskalkulator
            </button>
          </div>
        </div>
        <div class="hero-image">
          <img src="assets/images/hero-illustration.svg" alt="BNPL Illustration">
        </div>
      </section>

      <section class="features-section">
        <div class="section-header">
          <h2>Hvorfor velge YourCompany BNPL?</h2>
          <p>Fleksible betalingsløsninger tilpasset ditt behov</p>
        </div>
        
        <div class="features-grid">
          <mat-card class="feature-card">
            <mat-icon class="feature-icon">security</mat-icon>
            <h3>100% Trygt</h3>
            <p>SSL-kryptert og GDPR-kompatibel. Dine data er sikre hos oss.</p>
          </mat-card>

          <mat-card class="feature-card">
            <mat-icon class="feature-icon">flash_on</mat-icon>
            <h3>Rask godkjenning</h3>
            <p>Få svar på sekunder. Ingen lang ventetid eller papirarbeid.</p>
          </mat-card>

          <mat-card class="feature-card">
            <mat-icon class="feature-icon">account_balance_wallet</mat-icon>
            <h3>Fleksible planer</h3>
            <p>Velg mellom 3, 4, 6 eller 12 måneder. Tilpass til din økonomi.</p>
          </mat-card>

          <mat-card class="feature-card">
            <mat-icon class="feature-icon">support_agent</mat-icon>
            <h3>Norsk support</h3>
            <p>Kundeservice på norsk, tilgjengelig når du trenger hjelp.</p>
          </mat-card>
        </div>
      </section>

      <section class="merchants-section">
        <div class="section-header">
          <h2>Våre partnere</h2>
          <p>Handle hos tusenvis av norske nettbutikker</p>
        </div>
        
        <div class="merchants-grid">
          <div class="merchant-logo">
            <img src="assets/images/merchants/elkjop-logo.svg" alt="Elkjøp">
          </div>
          <div class="merchant-logo">
            <img src="assets/images/merchants/komplett-logo.svg" alt="Komplett">
          </div>
          <div class="merchant-logo">
            <img src="assets/images/merchants/power-logo.svg" alt="Power">
          </div>
          <div class="merchant-logo">
            <img src="assets/images/merchants/xxl-logo.svg" alt="XXL">
          </div>
        </div>
        
        <div class="merchants-cta">
          <button mat-button routerLink="/merchants">
            Se alle partnere
            <mat-icon>arrow_forward</mat-icon>
          </button>
        </div>
      </section>
    </div>
  `,
  styles: [`
    .home-container {
      min-height: 100vh;
    }

    .hero-section {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 48px;
      padding: 80px 24px;
      max-width: 1200px;
      margin: 0 auto;
      align-items: center;
    }

    .hero-content {
      .hero-title {
        font-size: 3rem;
        font-weight: 700;
        margin-bottom: 16px;
        background: var(--gradient-primary);
        -webkit-background-clip: text;
        -webkit-text-fill-color: transparent;
        background-clip: text;
      }

      .hero-subtitle {
        font-size: 1.25rem;
        color: var(--yourcompany-text-secondary);
        margin-bottom: 32px;
      }

      .hero-actions {
        display: flex;
        gap: 16px;
        flex-wrap: wrap;

        .cta-button {
          padding: 12px 32px;
          font-size: 16px;
          font-weight: 600;
        }

        .secondary-button {
          display: flex;
          align-items: center;
          gap: 8px;
        }
      }
    }

    .hero-image {
      text-align: center;

      img {
        max-width: 100%;
        height: auto;
      }
    }

    .features-section, .merchants-section {
      padding: 80px 24px;
      max-width: 1200px;
      margin: 0 auto;
    }

    .section-header {
      text-align: center;
      margin-bottom: 48px;

      h2 {
        font-size: 2.5rem;
        font-weight: 600;
        margin-bottom: 16px;
      }

      p {
        font-size: 1.125rem;
        color: var(--yourcompany-text-secondary);
      }
    }

    .features-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
      gap: 24px;
    }

    .feature-card {
      text-align: center;
      padding: 32px 24px;
      transition: transform 0.2s ease;

      &:hover {
        transform: translateY(-4px);
      }

      .feature-icon {
        font-size: 48px;
        width: 48px;
        height: 48px;
        color: var(--yourcompany-primary);
        margin-bottom: 16px;
      }

      h3 {
        font-size: 1.25rem;
        font-weight: 600;
        margin-bottom: 12px;
      }

      p {
        color: var(--yourcompany-text-secondary);
      }
    }

    .merchants-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(150px, 1fr));
      gap: 32px;
      margin-bottom: 32px;
    }

    .merchant-logo {
      display: flex;
      align-items: center;
      justify-content: center;
      padding: 24px;
      background: white;
      border-radius: 12px;
      box-shadow: var(--shadow-sm);

      img {
        max-width: 120px;
        max-height: 60px;
        opacity: 0.7;
        transition: opacity 0.2s ease;
      }

      &:hover img {
        opacity: 1;
      }
    }

    .merchants-cta {
      text-align: center;
    }

    @media (max-width: 768px) {
      .hero-section {
        grid-template-columns: 1fr;
        padding: 48px 16px;
        text-align: center;

        .hero-content .hero-title {
          font-size: 2.5rem;
        }
      }

      .features-section, .merchants-section {
        padding: 48px 16px;
      }

      .section-header h2 {
        font-size: 2rem;
      }
    }
  `]
})
export class HomeComponent {}