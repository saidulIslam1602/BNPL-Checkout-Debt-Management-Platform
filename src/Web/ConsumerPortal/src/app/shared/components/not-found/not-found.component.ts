import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [CommonModule, RouterModule, MatButtonModule, MatIconModule],
  template: `
    <div class="not-found-container">
      <div class="not-found-content">
        <mat-icon class="not-found-icon">error_outline</mat-icon>
        <h1 class="not-found-title">404</h1>
        <h2 class="not-found-subtitle">Siden ble ikke funnet</h2>
        <p class="not-found-message">
          Siden du leter etter eksisterer ikke eller har blitt flyttet.
        </p>
        <div class="not-found-actions">
          <button 
            mat-raised-button 
            color="primary" 
            routerLink="/"
            class="back-button">
            <mat-icon>home</mat-icon>
            Tilbake til forsiden
          </button>
          <button 
            mat-button 
            routerLink="/contact"
            class="contact-button">
            <mat-icon>support_agent</mat-icon>
            Kontakt support
          </button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .not-found-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 80vh;
      padding: 20px;
      background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
    }

    .not-found-content {
      text-align: center;
      max-width: 500px;
      padding: 48px 32px;
      background: white;
      border-radius: 16px;
      box-shadow: var(--shadow-lg);
    }

    .not-found-icon {
      font-size: 96px;
      width: 96px;
      height: 96px;
      color: var(--yourcompany-text-secondary);
      margin-bottom: 24px;
    }

    .not-found-title {
      font-size: 72px;
      font-weight: 700;
      color: var(--yourcompany-primary);
      margin: 0 0 16px 0;
      background: var(--gradient-primary);
      -webkit-background-clip: text;
      -webkit-text-fill-color: transparent;
      background-clip: text;
    }

    .not-found-subtitle {
      font-size: 24px;
      font-weight: 600;
      color: var(--yourcompany-text);
      margin: 0 0 16px 0;
    }

    .not-found-message {
      font-size: 16px;
      color: var(--yourcompany-text-secondary);
      margin: 0 0 32px 0;
      line-height: 1.5;
    }

    .not-found-actions {
      display: flex;
      flex-direction: column;
      gap: 12px;
      align-items: center;

      .back-button, .contact-button {
        display: flex;
        align-items: center;
        gap: 8px;
        min-width: 200px;
      }
    }

    @media (max-width: 480px) {
      .not-found-content {
        padding: 32px 24px;
        margin: 16px;
      }

      .not-found-title {
        font-size: 56px;
      }

      .not-found-subtitle {
        font-size: 20px;
      }

      .not-found-actions {
        .back-button, .contact-button {
          min-width: 100%;
        }
      }
    }
  `]
})
export class NotFoundComponent {}