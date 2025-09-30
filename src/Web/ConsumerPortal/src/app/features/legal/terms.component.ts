import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';

@Component({
  selector: 'app-terms',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule
  ],
  template: `
    <div class="terms-container">
      <div class="hero-section">
        <h1>Terms of Service</h1>
        <p class="subtitle">Last updated: {{lastUpdated | date:'longDate'}}</p>
      </div>

      <mat-card class="terms-content">
        <mat-card-content>
          <section>
            <h2>1. Agreement to Terms</h2>
            <p>
              By accessing and using YourCompany BNPL services, you accept and agree to be bound by the terms and provision of this agreement. 
              If you do not agree to abide by the above, please do not use this service.
            </p>
          </section>

          <section>
            <h2>2. BNPL Service Description</h2>
            <p>
              Our Buy Now, Pay Later service allows eligible customers to purchase goods and services from participating merchants 
              and pay for them in installments over time. The specific terms of each payment plan will be clearly disclosed 
              before you complete your purchase.
            </p>
          </section>

          <section>
            <h2>3. Eligibility Requirements</h2>
            <ul>
              <li>Must be at least 18 years of age</li>
              <li>Must be a resident of Norway</li>
              <li>Must have a valid Norwegian bank account</li>
              <li>Must pass our credit assessment process</li>
              <li>Must provide accurate and complete information</li>
            </ul>
          </section>

          <section>
            <h2>4. Payment Terms</h2>
            <p>
              Payment schedules and amounts are determined at the time of purchase based on the selected plan. 
              Payments are automatically debited from your chosen payment method on the scheduled dates. 
              You are responsible for ensuring sufficient funds are available.
            </p>
          </section>

          <section>
            <h2>5. Late Payments and Fees</h2>
            <p>
              Late payment fees may apply if payments are not made on time. We will provide advance notice 
              of any fees. Repeated late payments may affect your ability to use our service in the future 
              and may be reported to credit agencies.
            </p>
          </section>

          <section>
            <h2>6. Privacy and Data Protection</h2>
            <p>
              We are committed to protecting your privacy and personal data in accordance with GDPR and Norwegian 
              data protection laws. Please see our Privacy Policy for detailed information about how we collect, 
              use, and protect your information.
            </p>
          </section>

          <section>
            <h2>7. Merchant Relationships</h2>
            <p>
              We are not responsible for the quality, delivery, or customer service of goods and services 
              purchased from merchants. Disputes regarding products should be resolved directly with the merchant, 
              though we may assist in the resolution process.
            </p>
          </section>

          <section>
            <h2>8. Changes to Terms</h2>
            <p>
              We reserve the right to modify these terms at any time. Material changes will be communicated 
              to users via email or through our platform. Continued use of our service after changes 
              constitutes acceptance of the new terms.
            </p>
          </section>

          <section>
            <h2>9. Contact Information</h2>
            <p>
              If you have any questions about these Terms of Service, please contact us at:
            </p>
            <ul>
              <li>Email: legal&#64;yourcompanybnpl.no</li>
              <li>Phone: +47 800 12 345</li>
              <li>Address: Oslo, Norway</li>
            </ul>
          </section>
        </mat-card-content>
        <mat-card-actions>
          <button mat-button routerLink="/privacy">Privacy Policy</button>
          <button mat-button routerLink="/cookies">Cookie Policy</button>
        </mat-card-actions>
      </mat-card>
    </div>
  `,
  styles: [`
    .terms-container {
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

    .terms-content {
      section {
        margin-bottom: 2rem;
        
        h2 {
          color: #333;
          margin-bottom: 1rem;
          font-size: 1.3rem;
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
export class TermsComponent {
  lastUpdated = new Date('2024-01-01');
}