import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-cookies',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatSlideToggleModule,
    FormsModule
  ],
  template: `
    <div class="cookies-container">
      <div class="hero-section">
        <h1>Cookie Policy</h1>
        <p class="subtitle">Last updated: {{lastUpdated | date:'longDate'}}</p>
      </div>

      <mat-card class="cookies-content">
        <mat-card-content>
          <section>
            <h2>What Are Cookies?</h2>
            <p>
              Cookies are small text files that are placed on your device when you visit our website. 
              They help us provide you with a better experience by remembering your preferences and 
              analyzing how you use our service.
            </p>
          </section>

          <section>
            <h2>Types of Cookies We Use</h2>
            
            <div class="cookie-category">
              <h3>Essential Cookies</h3>
              <p>These cookies are necessary for the website to function properly and cannot be disabled.</p>
              <ul>
                <li>Authentication and security</li>
                <li>Shopping cart functionality</li>
                <li>Form submission and validation</li>
              </ul>
            </div>

            <div class="cookie-category">
              <h3>Analytics Cookies</h3>
              <p>Help us understand how visitors interact with our website.</p>
              <mat-slide-toggle [(ngModel)]="analyticsEnabled" (change)="updateCookiePreferences()">
                Allow Analytics Cookies
              </mat-slide-toggle>
            </div>

            <div class="cookie-category">
              <h3>Marketing Cookies</h3>
              <p>Used to deliver relevant advertisements and track campaign effectiveness.</p>
              <mat-slide-toggle [(ngModel)]="marketingEnabled" (change)="updateCookiePreferences()">
                Allow Marketing Cookies
              </mat-slide-toggle>
            </div>

            <div class="cookie-category">
              <h3>Functional Cookies</h3>
              <p>Enable enhanced functionality and personalization.</p>
              <mat-slide-toggle [(ngModel)]="functionalEnabled" (change)="updateCookiePreferences()">
                Allow Functional Cookies
              </mat-slide-toggle>
            </div>
          </section>

          <section>
            <h2>Third-Party Cookies</h2>
            <p>We may use third-party services that set their own cookies:</p>
            <ul>
              <li><strong>Google Analytics:</strong> Website usage analytics</li>
              <li><strong>Payment Processors:</strong> Secure payment processing</li>
              <li><strong>Customer Support:</strong> Live chat and support tools</li>
            </ul>
          </section>

          <section>
            <h2>Managing Your Cookie Preferences</h2>
            <p>
              You can control cookies through your browser settings or using the toggles above. 
              Note that disabling certain cookies may affect website functionality.
            </p>
            
            <div class="browser-instructions">
              <h4>Browser Cookie Settings:</h4>
              <ul>
                <li><strong>Chrome:</strong> Settings > Privacy and Security > Cookies</li>
                <li><strong>Firefox:</strong> Options > Privacy & Security > Cookies</li>
                <li><strong>Safari:</strong> Preferences > Privacy > Cookies</li>
                <li><strong>Edge:</strong> Settings > Cookies and site permissions</li>
              </ul>
            </div>
          </section>

          <section>
            <h2>Contact Us</h2>
            <p>
              If you have questions about our use of cookies, please contact us at 
              privacy&#64;yourcompanybnpl.no or +47 800 12 345.
            </p>
          </section>
        </mat-card-content>
        <mat-card-actions>
          <button mat-raised-button color="primary" (click)="savePreferences()">
            Save Preferences
          </button>
          <button mat-button (click)="acceptAll()">Accept All</button>
          <button mat-button (click)="rejectAll()">Reject All</button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .cookies-container {
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
        color: #666;
      }
    }

    .cookies-content {
      section {
        margin-bottom: 2rem;
        
        h2 {
          color: #333;
          margin-bottom: 1rem;
          font-size: 1.3rem;
        }
        
        h3 {
          color: #555;
          margin: 1rem 0 0.5rem 0;
          font-size: 1.1rem;
        }
        
        h4 {
          color: #666;
          margin: 1rem 0 0.5rem 0;
        }
        
        p, li {
          line-height: 1.6;
          color: #555;
        }
        
        ul {
          padding-left: 1.5rem;
          
          li {
            margin-bottom: 0.5rem;
          }
        }
      }
    }

    .cookie-category {
      background: #f9f9f9;
      padding: 1rem;
      border-radius: 4px;
      margin-bottom: 1rem;
      
      mat-slide-toggle {
        margin-top: 0.5rem;
      }
    }

    .browser-instructions {
      background: #e3f2fd;
      padding: 1rem;
      border-radius: 4px;
      margin-top: 1rem;
    }
  `]
})
export class CookiesComponent {
  lastUpdated = new Date('2024-01-01');
  analyticsEnabled = true;
  marketingEnabled = false;
  functionalEnabled = true;

  updateCookiePreferences() {
    // TODO: Update cookie preferences in service
    console.log('Cookie preferences updated');
  }

  savePreferences() {
    // TODO: Save preferences to backend
    console.log('Saving cookie preferences');
  }

  acceptAll() {
    this.analyticsEnabled = true;
    this.marketingEnabled = true;
    this.functionalEnabled = true;
    this.savePreferences();
  }

  rejectAll() {
    this.analyticsEnabled = false;
    this.marketingEnabled = false;
    this.functionalEnabled = false;
    this.savePreferences();
  }
}