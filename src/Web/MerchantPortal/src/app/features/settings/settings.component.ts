import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MatTabsModule } from '@angular/material/tabs';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ReactiveFormsModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatSlideToggleModule,
    MatTabsModule
  ],
  template: `
    <div class="settings-container">
      <div class="header-section">
        <h1>Merchant Settings</h1>
        <p class="subtitle">Configure your BNPL integration and preferences</p>
      </div>

      <mat-tab-group>
        <mat-tab label="Business Information">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>Business Details</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <form [formGroup]="businessForm">
                  <div class="form-row">
                    <mat-form-field appearance="outline">
                      <mat-label>Business Name</mat-label>
                      <input matInput formControlName="businessName">
                    </mat-form-field>
                    <mat-form-field appearance="outline">
                      <mat-label>Organization Number</mat-label>
                      <input matInput formControlName="orgNumber">
                    </mat-form-field>
                  </div>

                  <div class="form-row">
                    <mat-form-field appearance="outline">
                      <mat-label>Industry</mat-label>
                      <mat-select formControlName="industry">
                        @for (industry of industries(); track industry.value) {
                          <mat-option [value]="industry.value">{{industry.label}}</mat-option>
                        }
                      </mat-select>
                    </mat-form-field>
                    <mat-form-field appearance="outline">
                      <mat-label>Website URL</mat-label>
                      <input matInput formControlName="websiteUrl" type="url">
                    </mat-form-field>
                  </div>

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Business Description</mat-label>
                    <textarea matInput rows="3" formControlName="description"></textarea>
                  </mat-form-field>
                </form>
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary" (click)="saveBusiness()">
                  Save Changes
                </button>
              </mat-card-actions>
            </mat-card>
          </div>
        </mat-tab>

        <mat-tab label="BNPL Configuration">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>BNPL Settings</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <div class="setting-item">
                  <div class="setting-info">
                    <h3>Enable BNPL</h3>
                    <p>Allow customers to use Buy Now, Pay Later options</p>
                  </div>
                  <mat-slide-toggle [(ngModel)]="bnplEnabled">
                  </mat-slide-toggle>
                </div>

                <div class="setting-item">
                  <div class="setting-info">
                    <h3>Minimum Order Amount</h3>
                    <p>Minimum purchase amount for BNPL eligibility</p>
                  </div>
                  <mat-form-field appearance="outline">
                    <input matInput type="number" [(ngModel)]="minOrderAmount">
                    <span matSuffix>NOK</span>
                  </mat-form-field>
                </div>

                <div class="setting-item">
                  <div class="setting-info">
                    <h3>Maximum Order Amount</h3>
                    <p>Maximum purchase amount for BNPL</p>
                  </div>
                  <mat-form-field appearance="outline">
                    <input matInput type="number" [(ngModel)]="maxOrderAmount">
                    <span matSuffix>NOK</span>
                  </mat-form-field>
                </div>

                <div class="setting-item">
                  <div class="setting-info">
                    <h3>Available Plans</h3>
                    <p>Select which payment plans to offer customers</p>
                  </div>
                  <div class="plans-grid">
                    @for (plan of paymentPlans(); track plan.id) {
                      <div class="plan-option">
                        <mat-slide-toggle [(ngModel)]="plan.enabled">
                          {{plan.name}}
                        </mat-slide-toggle>
                        <span class="plan-description">{{plan.description}}</span>
                      </div>
                    }
                  </div>
                </div>
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary" (click)="saveBNPLConfig()">
                  Save Configuration
                </button>
              </mat-card-actions>
            </mat-card>
          </div>
        </mat-tab>

        <mat-tab label="Notifications">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>Notification Preferences</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                @for (notification of notificationSettings(); track notification.id) {
                  <div class="setting-item">
                    <div class="setting-info">
                      <h3>{{notification.title}}</h3>
                      <p>{{notification.description}}</p>
                    </div>
                    <mat-slide-toggle [(ngModel)]="notification.enabled">
                    </mat-slide-toggle>
                  </div>
                }
              </mat-card-content>
              <mat-card-actions>
                <button mat-raised-button color="primary" (click)="saveNotifications()">
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
    .settings-container {
      padding: 2rem;
      max-width: 1000px;
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

    .setting-item {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 1rem 0;
      border-bottom: 1px solid #eee;
      
      .setting-info {
        flex: 1;
        
        h3 {
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

    .plans-grid {
      display: grid;
      gap: 0.5rem;
    }

    .plan-option {
      display: flex;
      align-items: center;
      
      mat-slide-toggle {
        margin-right: 1rem;
      }
      
      .plan-description {
        color: #666;
        font-size: 0.9rem;
      }
    }

    .key-actions, .webhook-actions {
      display: flex;
      gap: 1rem;
      margin-top: 1rem;
    }
  `]
})
export class SettingsComponent {
  businessForm: FormGroup;
  bnplEnabled = true;
  minOrderAmount = 100;
  maxOrderAmount = 50000;

  industries = signal([
    { value: 'electronics', label: 'Electronics' },
    { value: 'fashion', label: 'Fashion & Clothing' },
    { value: 'home', label: 'Home & Garden' },
    { value: 'sports', label: 'Sports & Outdoors' },
    { value: 'other', label: 'Other' }
  ]);

  paymentPlans = signal([
    { id: 1, name: 'Pay in 3', description: '3 installments, 0% interest', enabled: true },
    { id: 2, name: 'Pay in 4', description: '4 installments, 0% interest', enabled: true },
    { id: 3, name: 'Pay in 6', description: '6 installments, 5.9% APR', enabled: false },
    { id: 4, name: 'Pay in 12', description: '12 installments, 9.9% APR', enabled: false }
  ]);

  notificationSettings = signal([
    { id: 1, title: 'Payment Notifications', description: 'Receive notifications when payments are processed', enabled: true },
    { id: 2, title: 'Settlement Alerts', description: 'Get notified when settlements are ready', enabled: true },
    { id: 3, title: 'Risk Alerts', description: 'Receive alerts for high-risk transactions', enabled: true },
    { id: 4, title: 'Weekly Reports', description: 'Receive weekly performance reports', enabled: false }
  ]);

  publicKey = signal('pk_live_51H7qYzGqjv4RXVH7...');
  secretKey = signal('sk_live_51H7qYzGqjv4RXVH7...');
  showSecretKey = signal(false);

  constructor(private fb: FormBuilder) {
    this.businessForm = this.fb.group({
      businessName: ['TechStore Norway AS', Validators.required],
      orgNumber: ['123456789', Validators.required],
      industry: ['electronics', Validators.required],
      websiteUrl: ['https://techstore.no', [Validators.required, Validators.pattern('https?://.+')]],
      description: ['Leading electronics retailer in Norway']
    });
  }

  toggleSecretKey() {
    this.showSecretKey.update(show => !show);
  }

  copyToClipboard(text: string) {
    navigator.clipboard.writeText(text);
  }

  regenerateKeys() {
    console.log('Regenerating keys...');
  }

  downloadKeys() {
    console.log('Downloading keys...');
  }

  saveWebhookConfig() {
    console.log('Saving webhook config...');
  }

  testWebhook() {
    console.log('Testing webhook...');
  }

  downloadSDK() {
    console.log('Downloading SDK...');
  }

  viewFullDocs() {
    window.open('https://docs.yourcompanybnpl.com', '_blank');
  }

  saveBusiness() {
    if (this.businessForm.valid) {
      console.log('Saving business info...', this.businessForm.value);
    }
  }

  saveBNPLConfig() {
    console.log('Saving BNPL config...');
  }

  saveNotifications() {
    console.log('Saving notification preferences...');
  }
}