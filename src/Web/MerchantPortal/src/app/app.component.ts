import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterOutlet, Router } from '@angular/router';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatIconModule } from '@angular/material/icon';
import { MatButtonModule } from '@angular/material/button';
import { MatListModule } from '@angular/material/list';
import { MatMenuModule } from '@angular/material/menu';
import { MatBadgeModule } from '@angular/material/badge';
import { MatProgressBarModule } from '@angular/material/progress-bar';

import { Store } from '@ngrx/store';
import { Observable } from 'rxjs';

import { AuthService } from './core/services/auth.service';
import { LoadingService } from './core/services/loading.service';
import { NotificationService } from './core/services/notification.service';
import { selectCurrentUser, selectIsAuthenticated } from './core/store/auth/auth.selectors';
import { User } from './core/models/user.model';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [
    CommonModule,
    RouterOutlet,
    MatToolbarModule,
    MatSidenavModule,
    MatIconModule,
    MatButtonModule,
    MatListModule,
    MatMenuModule,
    MatBadgeModule,
    MatProgressBarModule
  ],
  template: `
    <div class="app-container">
      <!-- Loading Bar -->
      <mat-progress-bar 
        *ngIf="isLoading$ | async" 
        mode="indeterminate" 
        class="loading-bar">
      </mat-progress-bar>

      <!-- Main Layout -->
      <mat-sidenav-container class="sidenav-container" [hasBackdrop]="false">
        <!-- Sidebar Navigation -->
        <mat-sidenav 
          #drawer 
          class="sidenav" 
          fixedInViewport 
          [attr.role]="'navigation'"
          [mode]="'side'"
          [opened]="isAuthenticated$ | async">
          
          <div class="sidenav-header">
            <img src="assets/images/riverty-logo.svg" alt="Riverty" class="logo">
            <h2>Merchant Portal</h2>
          </div>

          <mat-nav-list class="nav-list">
            <a mat-list-item routerLink="/dashboard" routerLinkActive="active">
              <mat-icon matListItemIcon>dashboard</mat-icon>
              <span matListItemTitle>Dashboard</span>
            </a>
            
            <a mat-list-item routerLink="/transactions" routerLinkActive="active">
              <mat-icon matListItemIcon>payment</mat-icon>
              <span matListItemTitle>Transactions</span>
              <mat-icon matListItemMeta *ngIf="pendingTransactions > 0" 
                       [matBadge]="pendingTransactions" 
                       matBadgeColor="warn">notifications</mat-icon>
            </a>
            
            <a mat-list-item routerLink="/settlements" routerLinkActive="active">
              <mat-icon matListItemIcon>account_balance</mat-icon>
              <span matListItemTitle>Settlements</span>
            </a>
            
            <a mat-list-item routerLink="/customers" routerLinkActive="active">
              <mat-icon matListItemIcon>people</mat-icon>
              <span matListItemTitle>Customers</span>
            </a>
            
            <a mat-list-item routerLink="/analytics" routerLinkActive="active">
              <mat-icon matListItemIcon>analytics</mat-icon>
              <span matListItemTitle>Analytics</span>
            </a>
            
            <a mat-list-item routerLink="/risk-management" routerLinkActive="active">
              <mat-icon matListItemIcon>security</mat-icon>
              <span matListItemTitle>Risk Management</span>
            </a>
            
            <mat-divider></mat-divider>
            
            <a mat-list-item routerLink="/integration" routerLinkActive="active">
              <mat-icon matListItemIcon>integration_instructions</mat-icon>
              <span matListItemTitle>Integration</span>
            </a>
            
            <a mat-list-item routerLink="/settings" routerLinkActive="active">
              <mat-icon matListItemIcon>settings</mat-icon>
              <span matListItemTitle>Settings</span>
            </a>
          </mat-nav-list>
        </mat-sidenav>

        <!-- Main Content -->
        <mat-sidenav-content class="main-content">
          <!-- Top Toolbar -->
          <mat-toolbar class="toolbar" color="primary">
            <button
              type="button"
              aria-label="Toggle sidenav"
              mat-icon-button
              (click)="drawer.toggle()"
              *ngIf="!(isAuthenticated$ | async)">
              <mat-icon aria-label="Side nav toggle icon">menu</mat-icon>
            </button>
            
            <span class="toolbar-title">{{ getPageTitle() }}</span>
            
            <div class="toolbar-spacer"></div>
            
            <!-- Norwegian Market Indicator -->
            <div class="market-indicator">
              <img src="assets/images/norway-flag.svg" alt="Norway" class="flag-icon">
              <span class="market-text">Norwegian Market</span>
            </div>
            
            <!-- User Menu -->
            <button mat-icon-button [matMenuTriggerFor]="userMenu" *ngIf="currentUser$ | async as user">
              <mat-icon>account_circle</mat-icon>
            </button>
            <mat-menu #userMenu="matMenu">
              <div class="user-info" *ngIf="currentUser$ | async as user">
                <div class="user-name">{{ user.firstName }} {{ user.lastName }}</div>
                <div class="user-email">{{ user.email }}</div>
                <div class="merchant-name">{{ user.merchantName }}</div>
              </div>
              <mat-divider></mat-divider>
              <button mat-menu-item routerLink="/profile">
                <mat-icon>person</mat-icon>
                <span>Profile</span>
              </button>
              <button mat-menu-item routerLink="/settings">
                <mat-icon>settings</mat-icon>
                <span>Settings</span>
              </button>
              <mat-divider></mat-divider>
              <button mat-menu-item (click)="logout()">
                <mat-icon>logout</mat-icon>
                <span>Logout</span>
              </button>
            </mat-menu>
          </mat-toolbar>

          <!-- Page Content -->
          <div class="content-container">
            <router-outlet></router-outlet>
          </div>
        </mat-sidenav-content>
      </mat-sidenav-container>
    </div>
  `,
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements OnInit {
  private store = inject(Store);
  private router = inject(Router);
  private authService = inject(AuthService);
  private loadingService = inject(LoadingService);
  private notificationService = inject(NotificationService);

  isAuthenticated$ = this.store.select(selectIsAuthenticated);
  currentUser$ = this.store.select(selectCurrentUser);
  isLoading$ = this.loadingService.loading$;

  pendingTransactions = 0;

  ngOnInit(): void {
    // Initialize app
    this.authService.initializeAuth();
    
    // Load pending transactions count
    this.loadPendingTransactions();
  }

  getPageTitle(): string {
    const url = this.router.url;
    const titleMap: { [key: string]: string } = {
      '/dashboard': 'Dashboard',
      '/transactions': 'Transactions',
      '/settlements': 'Settlements',
      '/customers': 'Customers',
      '/analytics': 'Analytics',
      '/risk-management': 'Risk Management',
      '/integration': 'Integration',
      '/settings': 'Settings',
      '/profile': 'Profile'
    };
    
    return titleMap[url] || 'Riverty Merchant Portal';
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }

  private loadPendingTransactions(): void {
    // This would typically come from a service
    // For now, we'll simulate some pending transactions
    this.pendingTransactions = 3;
  }
}