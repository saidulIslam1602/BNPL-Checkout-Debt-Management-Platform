import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatCheckboxModule,
    MatProgressSpinnerModule
  ],
  template: `
    <div class="login-container">
      <div class="login-card-container">
        <mat-card class="login-card">
          <mat-card-header class="login-header">
            <div class="logo-section">
              <img src="assets/images/yourcompany-logo.svg" alt="YourCompany" class="logo">
              <h1 class="app-title">Merchant Portal</h1>
            </div>
            <div class="market-indicator">
              <img src="assets/images/norway-flag.svg" alt="Norway" class="flag-icon">
              <span class="market-text">Norwegian Market</span>
            </div>
          </mat-card-header>

          <mat-card-content class="login-content">
            <form [formGroup]="loginForm" (ngSubmit)="onSubmit()" class="login-form">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Email</mat-label>
                <input 
                  matInput 
                  type="email" 
                  formControlName="email"
                  placeholder="Enter your email"
                  autocomplete="email">
                <mat-icon matSuffix>email</mat-icon>
                <mat-error *ngIf="loginForm.get('email')?.hasError('required')">
                  Email is required
                </mat-error>
                <mat-error *ngIf="loginForm.get('email')?.hasError('email')">
                  Please enter a valid email
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Password</mat-label>
                <input 
                  matInput 
                  [type]="hidePassword ? 'password' : 'text'"
                  formControlName="password"
                  placeholder="Enter your password"
                  autocomplete="current-password">
                <button 
                  mat-icon-button 
                  matSuffix 
                  type="button"
                  (click)="hidePassword = !hidePassword"
                  [attr.aria-label]="'Hide password'"
                  [attr.aria-pressed]="hidePassword">
                  <mat-icon>{{hidePassword ? 'visibility_off' : 'visibility'}}</mat-icon>
                </button>
                <mat-error *ngIf="loginForm.get('password')?.hasError('required')">
                  Password is required
                </mat-error>
              </mat-form-field>

              <div class="form-options">
                <mat-checkbox formControlName="rememberMe" class="remember-me">
                  Remember me
                </mat-checkbox>
                <a href="#" class="forgot-password-link" (click)="onForgotPassword($event)">
                  Forgot password?
                </a>
              </div>

              <button 
                mat-raised-button 
                color="primary" 
                type="submit"
                class="login-button full-width"
                [disabled]="loginForm.invalid || isLoading">
                <mat-spinner *ngIf="isLoading" diameter="20" class="login-spinner"></mat-spinner>
                <span *ngIf="!isLoading">Sign In</span>
                <span *ngIf="isLoading">Signing In...</span>
              </button>
            </form>
          </mat-card-content>

          <mat-card-actions class="login-actions">
            <div class="demo-credentials">
              <h4>Demo Credentials</h4>
              <p><strong>Email:</strong> merchant&#64;yourcompany.no</p>
              <p><strong>Password:</strong> demo123</p>
              <button 
                mat-button 
                color="accent" 
                (click)="fillDemoCredentials()"
                class="demo-button">
                Use Demo Credentials
              </button>
            </div>
          </mat-card-actions>
        </mat-card>
      </div>
    </div>
  `,
  styleUrls: ['./login.component.scss']
})
export class LoginComponent {
  private fb = inject(FormBuilder);
  private authService = inject(AuthService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);
  private notificationService = inject(NotificationService);

  loginForm: FormGroup;
  hidePassword = true;
  isLoading = false;

  constructor() {
    this.loginForm = this.fb.group({
      email: ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required]],
      rememberMe: [false]
    });
  }

  onSubmit(): void {
    if (this.loginForm.valid && !this.isLoading) {
      this.isLoading = true;
      
      const credentials = this.loginForm.value;
      
      this.authService.login(credentials).subscribe({
        next: (response) => {
          this.isLoading = false;
          this.notificationService.showSuccess('Welcome back!', 'Login Successful');
          
          // Redirect to return URL or dashboard
          const returnUrl = this.route.snapshot.queryParams['returnUrl'] || '/dashboard';
          this.router.navigate([returnUrl]);
        },
        error: (error) => {
          this.isLoading = false;
          const errorMessage = error.error?.message || 'Login failed. Please try again.';
          this.notificationService.showError(errorMessage, 'Login Failed');
        }
      });
    }
  }

  onForgotPassword(event: Event): void {
    event.preventDefault();
    this.notificationService.showInfo('Password reset functionality coming soon!', 'Forgot Password');
  }

  fillDemoCredentials(): void {
    this.loginForm.patchValue({
      email: 'merchant&#64;yourcompany.no',
      password: 'demo123',
      rememberMe: true
    });
  }
}