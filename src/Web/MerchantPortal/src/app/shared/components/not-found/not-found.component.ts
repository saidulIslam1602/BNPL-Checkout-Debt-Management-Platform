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
        <h2 class="not-found-subtitle">Page Not Found</h2>
        <p class="not-found-message">
          The page you're looking for doesn't exist or has been moved.
        </p>
        <button 
          mat-raised-button 
          color="primary" 
          routerLink="/dashboard"
          class="back-button">
          <mat-icon>home</mat-icon>
          Back to Dashboard
        </button>
      </div>
    </div>
  `,
  styles: [`
    .not-found-container {
      display: flex;
      align-items: center;
      justify-content: center;
      min-height: 100vh;
      background: linear-gradient(135deg, #f5f7fa 0%, #c3cfe2 100%);
      padding: 20px;
    }

    .not-found-content {
      text-align: center;
      max-width: 400px;
    }

    .not-found-icon {
      font-size: 72px;
      width: 72px;
      height: 72px;
      color: #64748b;
      margin-bottom: 16px;
    }

    .not-found-title {
      font-size: 72px;
      font-weight: 700;
      color: #1e293b;
      margin: 0 0 8px 0;
    }

    .not-found-subtitle {
      font-size: 24px;
      font-weight: 600;
      color: #475569;
      margin: 0 0 16px 0;
    }

    .not-found-message {
      font-size: 16px;
      color: #64748b;
      margin: 0 0 32px 0;
      line-height: 1.5;
    }

    .back-button {
      display: flex;
      align-items: center;
      gap: 8px;
    }
  `]
})
export class NotFoundComponent {}