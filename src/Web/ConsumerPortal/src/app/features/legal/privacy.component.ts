import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-privacy',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule
  ],
  template: `
    <div class="privacy-container">
      <div class="hero-section">
        <h1>Privacy Policy</h1>
        <p class="subtitle">Last updated: {{lastUpdated | date:'longDate'}}</p>
      </div>

      <mat-card class="privacy-content">
        <mat-card-content>
          <section>
            <h2>1. Information We Collect</h2>
            <p>We collect information you provide directly to us, such as when you create an account, apply for BNPL services, or contact us for support.</p>
            
            <h3>Personal Information:</h3>
            <ul>
              <li>Name, email address, phone number</li>
              <li>Date of birth and identification information</li>
              <li>Financial information for credit assessment</li>
              <li>Payment method and banking details</li>
            </ul>

            <h3>Usage Information:</h3>
            <ul>
              <li>Device and browser information</li>
              <li>IP address and location data</li>
              <li>Usage patterns and preferences</li>
              <li>Transaction history and payment behavior</li>
            </ul>
          </section>

          <section>
            <h2>2. How We Use Your Information</h2>
            <ul>
              <li>Process and manage your BNPL applications and payments</li>
              <li>Conduct credit assessments and risk analysis</li>
              <li>Communicate with you about your account and services</li>
              <li>Improve our services and user experience</li>
              <li>Comply with legal and regulatory requirements</li>
              <li>Prevent fraud and ensure platform security</li>
            </ul>
          </section>

          <section>
            <h2>3. Information Sharing</h2>
            <p>We do not sell your personal information. We may share your information in the following circumstances:</p>
            <ul>
              <li>With merchant partners to process transactions</li>
              <li>With credit bureaus for assessment and reporting</li>
              <li>With service providers who assist our operations</li>
              <li>When required by law or legal process</li>
              <li>To protect our rights and prevent fraud</li>
            </ul>
          </section>

          <section>
            <h2>4. Data Security</h2>
            <p>
              We implement appropriate technical and organizational measures to protect your personal information 
              against unauthorized access, alteration, disclosure, or destruction. This includes encryption, 
              secure servers, and regular security assessments.
            </p>
          </section>

          <section>
            <h2>5. Your Rights (GDPR)</h2>
            <p>Under GDPR and Norwegian data protection laws, you have the right to:</p>
            <ul>
              <li>Access your personal data</li>
              <li>Correct inaccurate information</li>
              <li>Request deletion of your data</li>
              <li>Object to processing</li>
              <li>Data portability</li>
              <li>Withdraw consent</li>
            </ul>
          </section>

          <section>
            <h2>6. Cookies and Tracking</h2>
            <p>
              We use cookies and similar technologies to enhance your experience, analyze usage, 
              and provide personalized content. See our Cookie Policy for detailed information.
            </p>
          </section>

          <section>
            <h2>7. Data Retention</h2>
            <p>
              We retain your information for as long as necessary to provide services, comply with legal obligations, 
              resolve disputes, and enforce agreements. Financial records may be retained for up to 7 years as required by law.
            </p>
          </section>

          <section>
            <h2>8. Contact Us</h2>
            <p>For privacy-related questions or to exercise your rights, contact us at:</p>
            <ul>
              <li>Email: privacy&#64;yourcompanybnpl.no</li>
              <li>Phone: +47 800 12 345</li>
              <li>Data Protection Officer: dpo&#64;yourcompanybnpl.no</li>
            </ul>
          </section>
        </mat-card-content>
        <mat-card-actions>
          <button mat-button routerLink="/terms">Terms of Service</button>
          <button mat-button routerLink="/cookies">Cookie Policy</button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .privacy-container {
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

    .privacy-content {
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
  `]
})
export class PrivacyComponent {
  lastUpdated = new Date('2024-01-01');
}