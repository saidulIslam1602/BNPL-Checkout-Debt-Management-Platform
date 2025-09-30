import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';

@Component({
  selector: 'app-profile',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule
  ],
  template: `
    <div class="profile-container">
      <div class="header-section">
        <h1>Merchant Profile</h1>
        <p class="subtitle">Manage your merchant account information</p>
      </div>

      <mat-tab-group>
        <mat-tab label="Account Information">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>Account Details</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <form [formGroup]="profileForm">
                  <div class="form-row">
                    <mat-form-field appearance="outline">
                      <mat-label>First Name</mat-label>
                      <input matInput formControlName="firstName">
                    </mat-form-field>
                    <mat-form-field appearance="outline">
                      <mat-label>Last Name</mat-label>
                      <input matInput formControlName="lastName">
                    </mat-form-field>
                  </div>

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Email Address</mat-label>
                    <input matInput type="email" formControlName="email">
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Phone Number</mat-label>
                    <input matInput formControlName="phone">
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Job Title</mat-label>
                    <input matInput formControlName="jobTitle">
                  </mat-form-field>
                </form>
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary" (click)="saveProfile()">
                  Save Changes
                </button>
              </mat-card-actions>
            </mat-card>
          </div>
        </mat-tab>

        <mat-tab label="Security">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>Password & Security</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <form [formGroup]="securityForm">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Current Password</mat-label>
                    <input matInput type="password" formControlName="currentPassword">
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>New Password</mat-label>
                    <input matInput type="password" formControlName="newPassword">
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Confirm New Password</mat-label>
                    <input matInput type="password" formControlName="confirmPassword">
                  </mat-form-field>
                </form>

                <div class="security-info">
                  <h3>Two-Factor Authentication</h3>
                  <p>Add an extra layer of security to your account</p>
                  <button mat-raised-button color="accent" (click)="setup2FA()">
                    Setup 2FA
                  </button>
                </div>
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary" (click)="changePassword()">
                  Change Password
                </button>
              </mat-card-actions>
            </mat-card>
          </div>
        </mat-tab>

        <mat-tab label="Preferences">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>Account Preferences</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <div class="preferences-grid">
                  @for (preference of preferences(); track preference.id) {
                    <div class="preference-item">
                      <div class="preference-info">
                        <h4>{{preference.title}}</h4>
                        <p>{{preference.description}}</p>
                      </div>
                      <mat-slide-toggle [(ngModel)]="preference.enabled">
                      </mat-slide-toggle>
                    </div>
                  }
                </div>
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary" (click)="savePreferences()">
                  Save Preferences
                </button>
              </mat-card-actions>
            </mat-card>
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [`
    .profile-container {
      padding: 2rem;
      max-width: 800px;
      margin: 0 auto;
    }

    .header-section {
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

    .tab-content {
      padding: 2rem 0;
    }

    .form-row {
      display: grid;
      grid-template-columns: 1fr 1fr;
      gap: 1rem;
      margin-bottom: 1rem;
      
      @media (max-width: 600px) {
        grid-template-columns: 1fr;
      }
    }

    .full-width {
      width: 100%;
      margin-bottom: 1rem;
    }

    .security-info {
      margin-top: 2rem;
      padding-top: 2rem;
      border-top: 1px solid #eee;
      
      h3 {
        margin-bottom: 0.5rem;
        color: #333;
      }
      
      p {
        margin-bottom: 1rem;
        color: #666;
      }
    }

    .preferences-grid {
      display: grid;
      gap: 1rem;
    }

    .preference-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem;
      border: 1px solid #eee;
      border-radius: 4px;
      
      .preference-info {
        flex: 1;
        
        h4 {
          margin: 0 0 0.5rem 0;
          color: #333;
        }
        
        p {
          margin: 0;
          color: #666;
          font-size: 0.9rem;
        }
      }
    }
  `]
})
export class ProfileComponent {
  profileForm: FormGroup;
  securityForm: FormGroup;

  preferences = signal([
    { id: 1, title: 'Email Notifications', description: 'Receive email updates about your account', enabled: true },
    { id: 2, title: 'SMS Notifications', description: 'Receive SMS alerts for important events', enabled: false },
    { id: 3, title: 'Marketing Communications', description: 'Receive promotional offers and updates', enabled: false },
    { id: 4, title: 'Weekly Reports', description: 'Receive weekly performance reports', enabled: true }
  ]);

  constructor(private fb: FormBuilder) {
    this.profileForm = this.fb.group({
      firstName: ['John', Validators.required],
      lastName: ['Doe', Validators.required],
      email: ['john.doe@techstore.no', [Validators.required, Validators.email]],
      phone: ['+47 12345678', Validators.required],
      jobTitle: ['Store Manager']
    });

    this.securityForm = this.fb.group({
      currentPassword: ['', Validators.required],
      newPassword: ['', [Validators.required, Validators.minLength(8)]],
      confirmPassword: ['', Validators.required]
    });
  }

  saveProfile() {
    if (this.profileForm.valid) {
      console.log('Saving profile...', this.profileForm.value);
    }
  }

  changePassword() {
    if (this.securityForm.valid) {
      console.log('Changing password...');
    }
  }

  setup2FA() {
    console.log('Setting up 2FA...');
  }

  savePreferences() {
    console.log('Saving preferences...');
  }
}