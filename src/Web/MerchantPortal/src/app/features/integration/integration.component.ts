import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatTabsModule } from '@angular/material/tabs';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { FormsModule } from '@angular/forms';

@Component({
  selector: 'app-integration',
  standalone: true,
  imports: [
    CommonModule,
    MatCardModule,
    MatButtonModule,
    MatIconModule,
    MatTabsModule,
    MatFormFieldModule,
    MatInputModule,
    MatSlideToggleModule,
    FormsModule
  ],
  template: `
    <div class="integration-container">
      <div class="header-section">
        <h1>API Integration</h1>
        <p class="subtitle">Integrate BNPL into your e-commerce platform</p>
      </div>

      <mat-tab-group>
        <mat-tab label="API Keys">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>API Credentials</mat-card-title>
                <mat-card-subtitle>Manage your API keys and webhooks</mat-card-subtitle>
              </mat-card-header>
              <mat-card-content>
                <div class="api-key-section">
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Public Key</mat-label>
                    <input matInput [value]="publicKey()" readonly>
                    <button matSuffix mat-icon-button (click)="copyToClipboard(publicKey())">
                      <mat-icon>content_copy</mat-icon>
                    </button>
                  </mat-form-field>

                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Secret Key</mat-label>
                    <input matInput [type]="showSecretKey() ? 'text' : 'password'" [value]="secretKey()" readonly>
                    <button matSuffix mat-icon-button (click)="toggleSecretKey()">
                      <mat-icon>{{showSecretKey() ? 'visibility_off' : 'visibility'}}</mat-icon>
                    </button>
                  </mat-form-field>

                  <div class="key-actions">
                    <button mat-raised-button color="primary" (click)="regenerateKeys()">
                      Regenerate Keys
                    </button>
                    <button mat-button (click)="downloadKeys()">
                      Download Keys
                    </button>
                  </div>
                </div>
              </mat-card-content>
            </mat-card>
          </div>
        </mat-tab>

        <mat-tab label="Webhooks">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>Webhook Configuration</mat-card-title>
                <mat-card-subtitle>Configure webhook endpoints for real-time updates</mat-card-subtitle>
              </mat-card-header>
              <mat-card-content>
                <mat-form-field appearance="outline" class="full-width">
                  <mat-label>Webhook URL</mat-label>
                  <input matInput [(ngModel)]="webhookUrl" placeholder="https://your-domain.com/webhooks/bnpl">
                </mat-form-field>

                <div class="webhook-events">
                  <h3>Events to Subscribe</h3>
                  @for (event of webhookEvents(); track event.id) {
                    <div class="event-toggle">
                      <mat-slide-toggle [(ngModel)]="event.enabled">
                        {{event.name}}
                      </mat-slide-toggle>
                      <span class="event-description">{{event.description}}</span>
                    </div>
                  }
                </div>

                <div class="webhook-actions">
                  <button mat-raised-button color="primary" (click)="saveWebhookConfig()">
                    Save Configuration
                  </button>
                  <button mat-button (click)="testWebhook()">
                    Test Webhook
                  </button>
                </div>
              </mat-card-content>
            </mat-card>
          </div>
        </mat-tab>

        <mat-tab label="Documentation">
          <div class="tab-content">
            <mat-card>
              <mat-card-header>
                <mat-card-title>Integration Guide</mat-card-title>
              </mat-card-header>
              <mat-card-content>
                <div class="docs-section">
                  <h3>Quick Start</h3>
                  <p>Follow these steps to integrate BNPL into your platform:</p>
                  <ol>
                    <li>Include our JavaScript SDK in your checkout page</li>
                    <li>Initialize the SDK with your public key</li>
                    <li>Create a payment session on your backend</li>
                    <li>Handle the payment response and webhooks</li>
                  </ol>

                  <h3>JavaScript SDK</h3>
                  <pre><code>{{sdkExample}}</code></pre>

                  <h3>Backend Integration</h3>
                  <pre><code>{{backendExample}}</code></pre>
                </div>
              </mat-card-content>
              <mat-card-actions>
                <button mat-button (click)="downloadSDK()">Download SDK</button>
                <button mat-button (click)="viewFullDocs()">Full Documentation</button>
              </mat-card-actions>
            </mat-card>
          </div>
        </mat-tab>
      </mat-tab-group>
    </div>
  `,
  styles: [`
    .integration-container {
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
      padding: 1rem 0;
    }

    .full-width {
      width: 100%;
      margin-bottom: 1rem;
    }

    .key-actions, .webhook-actions {
      display: flex;
      gap: 1rem;
      margin-top: 1rem;
    }

    .webhook-events {
      margin: 1.5rem 0;
      
      h3 {
        margin-bottom: 1rem;
        color: #333;
      }
    }

    .event-toggle {
      display: flex;
      align-items: center;
      margin-bottom: 1rem;
      
      mat-slide-toggle {
        margin-right: 1rem;
      }
      
      .event-description {
        color: #666;
        font-size: 0.9rem;
      }
    }

    .docs-section {
      h3 {
        color: #333;
        margin: 1.5rem 0 1rem 0;
      }
      
      pre {
        background: #f5f5f5;
        padding: 1rem;
        border-radius: 4px;
        overflow-x: auto;
        
        code {
          font-family: 'Courier New', monospace;
          font-size: 0.9rem;
        }
      }
      
      ol {
        padding-left: 1.5rem;
        
        li {
          margin-bottom: 0.5rem;
          line-height: 1.6;
        }
      }
    }
  `]
})
export class IntegrationComponent {
  publicKey = signal('pk_live_51H7...');
  secretKey = signal('sk_live_51H7...');
  showSecretKey = signal(false);
  webhookUrl = 'https://your-domain.com/webhooks/bnpl';

  webhookEvents = signal([
    { id: 1, name: 'payment.created', description: 'When a new payment is initiated', enabled: true },
    { id: 2, name: 'payment.completed', description: 'When a payment is successfully completed', enabled: true },
    { id: 3, name: 'payment.failed', description: 'When a payment fails', enabled: true },
    { id: 4, name: 'installment.due', description: 'When an installment is due', enabled: false },
    { id: 5, name: 'settlement.processed', description: 'When a settlement is processed', enabled: true }
  ]);

  sdkExample = `
<script src="https://js.yourcompanybnpl.com/v1/bnpl.js"></script>
<script>
  const bnpl = YourCompanyBNPL({
    publicKey: 'pk_live_51H7...',
    environment: 'production'
  });

  bnpl.createPayment({
    amount: 15000,
    currency: 'NOK',
    orderId: 'order_123',
    customer: {
      email: 'customer@example.com',
      phone: '+4712345678'
    }
  }).then(result => {
    if (result.success) {
      window.location.href = result.redirectUrl;
    }
  });
</script>`;

  backendExample = `
// Node.js/Express example
const bnpl = require('@yourcompany/bnpl-node');

app.post('/create-payment', async (req, res) => {
  try {
    const payment = await bnpl.payments.create({
      amount: req.body.amount,
      currency: 'NOK',
      customer: req.body.customer,
      metadata: { orderId: req.body.orderId }
    });
    
    res.json({ success: true, paymentId: payment.id });
  } catch (error) {
    res.status(400).json({ success: false, error: error.message });
  }
});`;

  toggleSecretKey() {
    this.showSecretKey.update(show => !show);
  }

  copyToClipboard(text: string) {
    navigator.clipboard.writeText(text);
    // TODO: Show success message
  }

  regenerateKeys() {
    // TODO: Implement key regeneration
    console.log('Regenerating API keys...');
  }

  downloadKeys() {
    // TODO: Implement key download
    console.log('Downloading keys...');
  }

  saveWebhookConfig() {
    // TODO: Save webhook configuration
    console.log('Saving webhook config...');
  }

  testWebhook() {
    // TODO: Test webhook endpoint
    console.log('Testing webhook...');
  }

  downloadSDK() {
    // TODO: Download SDK
    console.log('Downloading SDK...');
  }

  viewFullDocs() {
    // TODO: Open documentation
    window.open('https://docs.yourcompanybnpl.com', '_blank');
  }
}