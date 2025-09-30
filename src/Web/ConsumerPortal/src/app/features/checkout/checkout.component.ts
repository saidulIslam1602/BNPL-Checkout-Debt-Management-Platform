import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatStepperModule } from '@angular/material/stepper';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatChipsModule } from '@angular/material/chips';
import { MatDividerModule } from '@angular/material/divider';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatRadioModule } from '@angular/material/radio';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatNativeDateModule } from '@angular/material/core';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { RouterModule } from '@angular/router';

import { Observable, of } from 'rxjs';
import { map, startWith } from 'rxjs/operators';

import { CheckoutService } from './checkout.service';
import { PaymentService } from '../../core/services/payment.service';
import { RiskAssessmentService } from '../../core/services/risk-assessment.service';

interface CheckoutItem {
  id: string;
  name: string;
  description: string;
  price: number;
  quantity: number;
  imageUrl: string;
  merchantName: string;
}

interface BNPLOption {
  type: 'PAY_IN_3' | 'PAY_IN_4' | 'PAY_IN_6' | 'PAY_IN_12' | 'PAY_IN_24';
  displayName: string;
  installmentAmount: number;
  totalAmount: number;
  interestRate: number;
  downPayment: number;
  monthlyPayment: number;
  firstPaymentDate: Date;
  description: string;
  isRecommended: boolean;
  eligibilityScore: number;
}

interface CustomerInfo {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  dateOfBirth: Date;
  socialSecurityNumber: string;
  address: {
    street: string;
    city: string;
    postalCode: string;
    country: string;
  };
}

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    RouterModule,
    MatCardModule,
    MatStepperModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatIconModule,
    MatProgressBarModule,
    MatChipsModule,
    MatDividerModule,
    MatCheckboxModule,
    MatRadioModule,
    MatDatepickerModule,
    MatNativeDateModule,
    MatSnackBarModule
  ],
  template: `
    <div class="checkout-container">
      <!-- Header -->
      <div class="checkout-header">
        <div class="header-content">
          <img src="assets/images/yourcompany-logo.svg" alt="YourCompany" class="logo">
          <div class="header-text">
            <h1>Trygg BNPL-betaling</h1>
            <p>Kjøp nå, betal over tid med YourCompany</p>
          </div>
        </div>
        <div class="security-badges">
          <div class="badge">
            <mat-icon>security</mat-icon>
            <span>256-bit SSL</span>
          </div>
          <div class="badge">
            <mat-icon>verified_user</mat-icon>
            <span>GDPR-sikret</span>
          </div>
          <div class="badge">
            <img src="assets/images/norway-flag.svg" alt="Norge" class="flag-icon">
            <span>Norsk regulert</span>
          </div>
        </div>
      </div>

      <div class="checkout-content">
        <!-- Order Summary Sidebar -->
        <div class="order-summary">
          <mat-card class="summary-card">
            <mat-card-header>
              <mat-card-title>Handlekurv</mat-card-title>
              <mat-card-subtitle>{{ items().length }} {{ items().length === 1 ? 'vare' : 'varer' }}</mat-card-subtitle>
            </mat-card-header>
            <mat-card-content>
              <div class="order-items">
                <div class="order-item" *ngFor="let item of items()">
                  <img [src]="item.imageUrl" [alt]="item.name" class="item-image">
                  <div class="item-details">
                    <div class="item-name">{{ item.name }}</div>
                    <div class="item-merchant">{{ item.merchantName }}</div>
                    <div class="item-quantity">Antall: {{ item.quantity }}</div>
                  </div>
                  <div class="item-price">{{ item.price * item.quantity | currency:'NOK':'symbol':'1.0-0' }}</div>
                </div>
              </div>
              
              <mat-divider></mat-divider>
              
              <div class="order-totals">
                <div class="total-line">
                  <span>Subtotal:</span>
                  <span>{{ subtotal() | currency:'NOK':'symbol':'1.0-0' }}</span>
                </div>
                <div class="total-line">
                  <span>Frakt:</span>
                  <span>{{ shipping() | currency:'NOK':'symbol':'1.0-0' }}</span>
                </div>
                <div class="total-line">
                  <span>MVA (25%):</span>
                  <span>{{ tax() | currency:'NOK':'symbol':'1.0-0' }}</span>
                </div>
                <div class="total-line total">
                  <span>Totalt:</span>
                  <span>{{ total() | currency:'NOK':'symbol':'1.0-0' }}</span>
                </div>
              </div>
            </mat-card-content>
          </mat-card>

          <!-- Selected BNPL Plan -->
          <mat-card class="selected-plan-card" *ngIf="selectedBNPLOption()">
            <mat-card-header>
              <mat-card-title>Valgt betalingsplan</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <div class="selected-plan">
                <div class="plan-name">{{ selectedBNPLOption()?.displayName }}</div>
                <div class="plan-details">
                  <div class="detail-line">
                    <span>Første betaling:</span>
                    <span>{{ selectedBNPLOption()?.downPayment | currency:'NOK':'symbol':'1.0-0' }}</span>
                  </div>
                  <div class="detail-line">
                    <span>Månedlig betaling:</span>
                    <span>{{ selectedBNPLOption()?.monthlyPayment | currency:'NOK':'symbol':'1.0-0' }}</span>
                  </div>
                  <div class="detail-line">
                    <span>Rente:</span>
                    <span>{{ selectedBNPLOption()?.interestRate }}% p.a.</span>
                  </div>
                  <div class="detail-line total">
                    <span>Totalt å betale:</span>
                    <span>{{ selectedBNPLOption()?.totalAmount | currency:'NOK':'symbol':'1.0-0' }}</span>
                  </div>
                </div>
              </div>
            </mat-card-content>
          </mat-card>
        </div>

        <!-- Main Checkout Flow -->
        <div class="checkout-main">
          <mat-stepper [linear]="true" #stepper class="checkout-stepper">
            
            <!-- Step 1: Customer Information -->
            <mat-step [stepControl]="customerForm" label="Personopplysninger">
              <form [formGroup]="customerForm" class="step-form">
                <div class="form-section">
                  <h3>Kontaktinformasjon</h3>
                  <div class="form-row">
                    <mat-form-field appearance="outline" class="half-width">
                      <mat-label>Fornavn</mat-label>
                      <input matInput formControlName="firstName" placeholder="Ditt fornavn">
                      <mat-error *ngIf="customerForm.get('firstName')?.hasError('required')">
                        Fornavn er påkrevd
                      </mat-error>
                    </mat-form-field>
                    <mat-form-field appearance="outline" class="half-width">
                      <mat-label>Etternavn</mat-label>
                      <input matInput formControlName="lastName" placeholder="Ditt etternavn">
                      <mat-error *ngIf="customerForm.get('lastName')?.hasError('required')">
                        Etternavn er påkrevd
                      </mat-error>
                    </mat-form-field>
                  </div>
                  
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>E-postadresse</mat-label>
                    <input matInput type="email" formControlName="email" placeholder="din.epost@example.no">
                    <mat-error *ngIf="customerForm.get('email')?.hasError('required')">
                      E-postadresse er påkrevd
                    </mat-error>
                    <mat-error *ngIf="customerForm.get('email')?.hasError('email')">
                      Ugyldig e-postadresse
                    </mat-error>
                  </mat-form-field>
                  
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Telefonnummer</mat-label>
                      <input matInput formControlName="phoneNumber" placeholder="+47 XXX XX XXX">
                    <mat-error *ngIf="customerForm.get('phoneNumber')?.hasError('required')">
                      Telefonnummer er påkrevd
                    </mat-error>
                  </mat-form-field>
                </div>

                <div class="form-section">
                  <h3>Personlig informasjon</h3>
                  <div class="form-row">
                    <mat-form-field appearance="outline" class="half-width">
                      <mat-label>Fødselsdato</mat-label>
                      <input matInput [matDatepicker]="picker" formControlName="dateOfBirth">
                      <mat-datepicker-toggle matIconSuffix [for]="picker"></mat-datepicker-toggle>
                      <mat-datepicker #picker></mat-datepicker>
                      <mat-error *ngIf="customerForm.get('dateOfBirth')?.hasError('required')">
                        Fødselsdato er påkrevd
                      </mat-error>
                    </mat-form-field>
                    <mat-form-field appearance="outline" class="half-width">
                      <mat-label>Personnummer</mat-label>
                      <input matInput formControlName="socialSecurityNumber" placeholder="DDMMYY XXXXX">
                      <mat-error *ngIf="customerForm.get('socialSecurityNumber')?.hasError('required')">
                        Personnummer er påkrevd
                      </mat-error>
                      <mat-error *ngIf="customerForm.get('socialSecurityNumber')?.hasError('pattern')">
                        Ugyldig personnummer format
                      </mat-error>
                    </mat-form-field>
                  </div>
                </div>

                <div class="form-section">
                  <h3>Adresse</h3>
                  <mat-form-field appearance="outline" class="full-width">
                    <mat-label>Gateadresse</mat-label>
                      <input matInput formControlName="street" placeholder="Gateadresse og nummer">
                    <mat-error *ngIf="customerForm.get('street')?.hasError('required')">
                      Gateadresse er påkrevd
                    </mat-error>
                  </mat-form-field>
                  
                  <div class="form-row">
                    <mat-form-field appearance="outline" class="half-width">
                      <mat-label>Postnummer</mat-label>
                      <input matInput formControlName="postalCode" placeholder="XXXX">
                      <mat-error *ngIf="customerForm.get('postalCode')?.hasError('required')">
                        Postnummer er påkrevd
                      </mat-error>
                    </mat-form-field>
                    <mat-form-field appearance="outline" class="half-width">
                      <mat-label>Poststed</mat-label>
                      <input matInput formControlName="city" placeholder="Poststed">
                      <mat-error *ngIf="customerForm.get('city')?.hasError('required')">
                        Poststed er påkrevd
                      </mat-error>
                    </mat-form-field>
                  </div>
                </div>

                <div class="step-actions">
                  <button mat-raised-button color="primary" 
                          [disabled]="!customerForm.valid || isProcessing()"
                          (click)="proceedToPaymentOptions()">
                    <mat-icon *ngIf="isProcessing()">hourglass_empty</mat-icon>
                    <span>{{ isProcessing() ? 'Behandler...' : 'Fortsett til betalingsalternativer' }}</span>
                  </button>
                </div>
              </form>
            </mat-step>

            <!-- Step 2: BNPL Options -->
            <mat-step label="Betalingsalternativer" [completed]="selectedBNPLOption() !== null">
              <div class="step-form">
                <div class="bnpl-options-header">
                  <h3>Velg din betalingsplan</h3>
                  <p>Basert på din kredittvurdering har vi funnet disse alternativene for deg:</p>
                </div>

                <div class="bnpl-options" *ngIf="bnplOptions().length > 0; else noOptions">
                  <div class="bnpl-option" 
                       *ngFor="let option of bnplOptions()" 
                       [class.selected]="selectedBNPLOption() === option"
                       [class.recommended]="option.isRecommended"
                       (click)="selectBNPLOption(option)">
                    
                    <div class="option-header">
                      <div class="option-title">
                        <span class="option-name">{{ option.displayName }}</span>
                        <mat-chip *ngIf="option.isRecommended" class="recommended-chip">Anbefalt</mat-chip>
                      </div>
                      <div class="option-rate">{{ option.interestRate }}% rente</div>
                    </div>

                    <div class="option-details">
                      <div class="payment-breakdown">
                        <div class="payment-item">
                          <span class="label">I dag:</span>
                          <span class="amount">{{ option.downPayment | currency:'NOK':'symbol':'1.0-0' }}</span>
                        </div>
                        <div class="payment-item">
                          <span class="label">Månedlig:</span>
                          <span class="amount">{{ option.monthlyPayment | currency:'NOK':'symbol':'1.0-0' }}</span>
                        </div>
                        <div class="payment-item total">
                          <span class="label">Totalt:</span>
                          <span class="amount">{{ option.totalAmount | currency:'NOK':'symbol':'1.0-0' }}</span>
                        </div>
                      </div>
                      
                      <div class="option-description">{{ option.description }}</div>
                      
                      <div class="eligibility-score">
                        <span class="score-label">Godkjenningssannsynlighet:</span>
                        <div class="score-bar">
                          <div class="score-fill" [style.width.%]="option.eligibilityScore"></div>
                        </div>
                        <span class="score-text">{{ option.eligibilityScore }}%</span>
                      </div>
                    </div>

                    <mat-radio-button [value]="option" 
                                      [checked]="selectedBNPLOption() === option"
                                      class="option-radio">
                      Velg denne planen
                    </mat-radio-button>
                  </div>
                </div>

                <ng-template #noOptions>
                  <div class="no-options">
                    <mat-icon class="no-options-icon">info</mat-icon>
                    <h4>Ingen BNPL-alternativer tilgjengelig</h4>
                    <p>Basert på din kredittvurdering kan vi dessverre ikke tilby BNPL for dette kjøpet.</p>
                    <p>Du kan fortsatt betale med kort eller Vipps.</p>
                  </div>
                </ng-template>

                <div class="step-actions">
                  <button mat-button (click)="stepper.previous()">Tilbake</button>
                  <button mat-raised-button color="primary" 
                          [disabled]="!selectedBNPLOption()"
                          (click)="stepper.next()">
                    Fortsett til bekreftelse
                  </button>
                </div>
              </div>
            </mat-step>

            <!-- Step 3: Confirmation -->
            <mat-step label="Bekreftelse">
              <div class="step-form">
                <div class="confirmation-header">
                  <h3>Bekreft ditt kjøp</h3>
                  <p>Vennligst gjennomgå detaljene før du fullfører kjøpet.</p>
                </div>

                <div class="confirmation-sections">
                  <!-- Customer Info Summary -->
                  <mat-card class="confirmation-card">
                    <mat-card-header>
                      <mat-card-title>Kundeinfo</mat-card-title>
                    </mat-card-header>
                    <mat-card-content>
                      <div class="info-grid">
                        <div class="info-item">
                          <span class="label">Navn:</span>
                          <span>{{ customerForm.get('firstName')?.value }} {{ customerForm.get('lastName')?.value }}</span>
                        </div>
                        <div class="info-item">
                          <span class="label">E-post:</span>
                          <span>{{ customerForm.get('email')?.value }}</span>
                        </div>
                        <div class="info-item">
                          <span class="label">Telefon:</span>
                          <span>{{ customerForm.get('phoneNumber')?.value }}</span>
                        </div>
                        <div class="info-item">
                          <span class="label">Adresse:</span>
                          <span>{{ customerForm.get('street')?.value }}, {{ customerForm.get('postalCode')?.value }} {{ customerForm.get('city')?.value }}</span>
                        </div>
                      </div>
                    </mat-card-content>
                  </mat-card>

                  <!-- Payment Plan Summary -->
                  <mat-card class="confirmation-card" *ngIf="selectedBNPLOption()">
                    <mat-card-header>
                      <mat-card-title>Betalingsplan</mat-card-title>
                    </mat-card-header>
                    <mat-card-content>
                      <div class="payment-schedule">
                        <div class="schedule-item first-payment">
                          <div class="payment-date">I dag</div>
                          <div class="payment-amount">{{ selectedBNPLOption()?.downPayment | currency:'NOK':'symbol':'1.0-0' }}</div>
                          <div class="payment-status">Betales nå</div>
                        </div>
                        
                        <div class="schedule-item" *ngFor="let payment of getPaymentSchedule(); let i = index">
                          <div class="payment-date">{{ payment.date | date:'dd.MM.yyyy' }}</div>
                          <div class="payment-amount">{{ payment.amount | currency:'NOK':'symbol':'1.0-0' }}</div>
                          <div class="payment-status">Automatisk trekk</div>
                        </div>
                      </div>
                    </mat-card-content>
                  </mat-card>

                  <!-- Terms and Conditions -->
                  <mat-card class="confirmation-card">
                    <mat-card-header>
                      <mat-card-title>Vilkår og betingelser</mat-card-title>
                    </mat-card-header>
                    <mat-card-content>
                      <div class="terms-section">
                        <mat-checkbox formControlName="acceptTerms" class="terms-checkbox">
                          Jeg godtar <a href="/terms" target="_blank">vilkårene og betingelsene</a> for YourCompany BNPL
                        </mat-checkbox>
                        
                        <mat-checkbox formControlName="acceptPrivacy" class="terms-checkbox">
                          Jeg godtar <a href="/privacy" target="_blank">personvernerklæringen</a> og behandling av personopplysninger
                        </mat-checkbox>
                        
                        <mat-checkbox formControlName="acceptCreditCheck" class="terms-checkbox">
                          Jeg godtar at YourCompany utfører kredittvurdering og kontakter kredittopplysningsforetak
                        </mat-checkbox>
                        
                        <mat-checkbox formControlName="acceptMarketing" class="terms-checkbox">
                          Jeg ønsker å motta tilbud og informasjon fra YourCompany (valgfritt)
                        </mat-checkbox>
                      </div>
                    </mat-card-content>
                  </mat-card>
                </div>

                <div class="step-actions">
                  <button mat-button (click)="stepper.previous()">Tilbake</button>
                  <button mat-raised-button color="primary" 
                          [disabled]="!canCompleteOrder() || isProcessing()"
                          (click)="completeOrder()"
                          class="complete-order-btn">
                    <mat-icon *ngIf="isProcessing()">hourglass_empty</mat-icon>
                    <span>{{ isProcessing() ? 'Behandler bestilling...' : 'Fullfør kjøp' }}</span>
                  </button>
                </div>
              </div>
            </mat-step>

            <!-- Step 4: Success -->
            <mat-step label="Fullført" [completed]="orderCompleted()">
              <div class="success-content" *ngIf="orderCompleted()">
                <div class="success-icon">
                  <mat-icon>check_circle</mat-icon>
                </div>
                <h2>Takk for ditt kjøp!</h2>
                <p>Din bestilling er bekreftet og du vil motta en e-post med detaljer.</p>
                
                <div class="order-details">
                  <div class="detail-item">
                    <span class="label">Ordrenummer:</span>
                    <span class="value">{{ orderNumber() }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="label">Første betaling:</span>
                    <span class="value">{{ selectedBNPLOption()?.downPayment | currency:'NOK':'symbol':'1.0-0' }}</span>
                  </div>
                  <div class="detail-item">
                    <span class="label">Neste betaling:</span>
                    <span class="value">{{ getNextPaymentDate() | date:'dd.MM.yyyy' }}</span>
                  </div>
                </div>

                <div class="success-actions">
                  <button mat-raised-button color="primary" routerLink="/my-payments">
                    Se mine betalinger
                  </button>
                  <button mat-button routerLink="/">
                    Tilbake til forsiden
                  </button>
                </div>
              </div>
            </mat-step>
          </mat-stepper>
        </div>
      </div>
    </div>
  `,
  styleUrls: ['./checkout.component.scss']
})
export class CheckoutComponent implements OnInit {
  private fb = inject(FormBuilder);
  private checkoutService = inject(CheckoutService);
  private paymentService = inject(PaymentService);
  private riskAssessmentService = inject(RiskAssessmentService);

  // Signals for reactive state management
  items = signal<CheckoutItem[]>([]);
  bnplOptions = signal<BNPLOption[]>([]);
  selectedBNPLOption = signal<BNPLOption | null>(null);
  isProcessing = signal(false);
  orderCompleted = signal(false);
  orderNumber = signal<string>('');

  // Computed values
  subtotal = computed(() => this.items().reduce((sum, item) => sum + (item.price * item.quantity), 0));
  shipping = computed(() => 99); // Fixed shipping cost
  tax = computed(() => Math.round(this.subtotal() * 0.25)); // 25% Norwegian VAT
  total = computed(() => this.subtotal() + this.shipping() + this.tax());

  // Forms
  customerForm: FormGroup;
  termsForm: FormGroup;

  constructor() {
    this.customerForm = this.fb.group({
      firstName: ['', [Validators.required, Validators.minLength(2)]],
      lastName: ['', [Validators.required, Validators.minLength(2)]],
      email: ['', [Validators.required, Validators.email]],
      phoneNumber: ['', [Validators.required, Validators.pattern(/^\+47\s?\d{3}\s?\d{2}\s?\d{3}$/)]],
      dateOfBirth: ['', Validators.required],
      socialSecurityNumber: ['', [Validators.required, Validators.pattern(/^\d{6}\s?\d{5}$/)]],
      street: ['', Validators.required],
      postalCode: ['', [Validators.required, Validators.pattern(/^\d{4}$/)]],
      city: ['', Validators.required],
      country: ['Norge']
    });

    this.termsForm = this.fb.group({
      acceptTerms: [false, Validators.requiredTrue],
      acceptPrivacy: [false, Validators.requiredTrue],
      acceptCreditCheck: [false, Validators.requiredTrue],
      acceptMarketing: [false]
    });
  }

  ngOnInit(): void {
    this.loadCheckoutData();
  }

  private loadCheckoutData(): void {
    // Load items from checkout service (would come from cart/session)
    this.items.set([
      {
        id: '1',
        name: 'iPhone 15 Pro 128GB',
        description: 'Titanium Natural',
        price: 13990,
        quantity: 1,
        imageUrl: 'assets/images/iphone-15-pro.jpg',
        merchantName: 'Elkjøp'
      },
      {
        id: '2',
        name: 'AirPods Pro (2nd generation)',
        description: 'Med MagSafe Charging Case',
        price: 2990,
        quantity: 1,
        imageUrl: 'assets/images/airpods-pro.jpg',
        merchantName: 'Elkjøp'
      }
    ]);
  }

  async proceedToPaymentOptions(): Promise<void> {
    if (!this.customerForm.valid) return;

    this.isProcessing.set(true);

    try {
      // Perform risk assessment
      const customerData = this.customerForm.value;
      const riskAssessment = await this.riskAssessmentService.assessCustomer({
        ...customerData,
        requestedAmount: this.total()
      }).toPromise();

      // Get BNPL options based on risk assessment
      const options = await this.getBNPLOptions(riskAssessment);
      this.bnplOptions.set(options);

    } catch (error) {
      console.error('Error during risk assessment:', error);
      // Handle error - show fallback options or error message
    } finally {
      this.isProcessing.set(false);
    }
  }

  private async getBNPLOptions(riskAssessment: any): Promise<BNPLOption[]> {
    const totalAmount = this.total();
    const options: BNPLOption[] = [];

    // Generate options based on risk score and amount
    if (riskAssessment.creditScore >= 700) {
      options.push({
        type: 'PAY_IN_3',
        displayName: 'Betal i 3 deler',
        installmentAmount: totalAmount / 3,
        totalAmount: totalAmount,
        interestRate: 0,
        downPayment: totalAmount / 3,
        monthlyPayment: totalAmount / 3,
        firstPaymentDate: new Date(),
        description: 'Ingen renter eller gebyrer. Betal 1/3 i dag, resten over 2 måneder.',
        isRecommended: true,
        eligibilityScore: 95
      });

      options.push({
        type: 'PAY_IN_4',
        displayName: 'Betal i 4 deler',
        installmentAmount: totalAmount / 4,
        totalAmount: totalAmount,
        interestRate: 0,
        downPayment: totalAmount / 4,
        monthlyPayment: totalAmount / 4,
        firstPaymentDate: new Date(),
        description: 'Ingen renter eller gebyrer. Betal 1/4 i dag, resten over 3 måneder.',
        isRecommended: false,
        eligibilityScore: 92
      });
    }

    if (riskAssessment.creditScore >= 600) {
      const interestRate = 0.05;
      const totalWithInterest = totalAmount * (1 + interestRate);

      options.push({
        type: 'PAY_IN_6',
        displayName: 'Betal over 6 måneder',
        installmentAmount: totalWithInterest / 6,
        totalAmount: totalWithInterest,
        interestRate: 5.0,
        downPayment: totalAmount * 0.2,
        monthlyPayment: (totalWithInterest - (totalAmount * 0.2)) / 5,
        firstPaymentDate: new Date(),
        description: '5% årlig rente. Fleksibel nedbetaling over 6 måneder.',
        isRecommended: false,
        eligibilityScore: 85
      });
    }

    if (riskAssessment.creditScore >= 550) {
      const interestRate = 0.12;
      const totalWithInterest = totalAmount * (1 + interestRate);

      options.push({
        type: 'PAY_IN_12',
        displayName: 'Betal over 12 måneder',
        installmentAmount: totalWithInterest / 12,
        totalAmount: totalWithInterest,
        interestRate: 12.0,
        downPayment: totalAmount * 0.15,
        monthlyPayment: (totalWithInterest - (totalAmount * 0.15)) / 11,
        firstPaymentDate: new Date(),
        description: '12% årlig rente. Lave månedlige betalinger over ett år.',
        isRecommended: false,
        eligibilityScore: 75
      });
    }

    return options;
  }

  selectBNPLOption(option: BNPLOption): void {
    this.selectedBNPLOption.set(option);
  }

  getPaymentSchedule(): Array<{date: Date, amount: number}> {
    const option = this.selectedBNPLOption();
    if (!option) return [];

    const schedule = [];
    const startDate = new Date();
    startDate.setMonth(startDate.getMonth() + 1);

    const installmentCount = option.type === 'PAY_IN_3' ? 2 : 
                           option.type === 'PAY_IN_4' ? 3 :
                           option.type === 'PAY_IN_6' ? 5 :
                           option.type === 'PAY_IN_12' ? 11 : 23;

    for (let i = 0; i < installmentCount; i++) {
      const paymentDate = new Date(startDate);
      paymentDate.setMonth(paymentDate.getMonth() + i);
      
      schedule.push({
        date: paymentDate,
        amount: option.monthlyPayment
      });
    }

    return schedule;
  }

  canCompleteOrder(): boolean {
    return this.customerForm.valid && 
           this.selectedBNPLOption() !== null && 
           this.termsForm.get('acceptTerms')?.value &&
           this.termsForm.get('acceptPrivacy')?.value &&
           this.termsForm.get('acceptCreditCheck')?.value;
  }

  async completeOrder(): Promise<void> {
    if (!this.canCompleteOrder()) return;

    this.isProcessing.set(true);

    try {
      const orderData = {
        customer: this.customerForm.value,
        items: this.items(),
        bnplOption: this.selectedBNPLOption(),
        total: this.total(),
        terms: this.termsForm.value
      };

      const result = await this.paymentService.createBNPLPayment(orderData).toPromise();
      
      if (result?.success) {
        this.orderNumber.set(result.orderNumber);
        this.orderCompleted.set(true);
      } else {
        throw new Error(result?.errorMessage || 'Payment failed');
      }

    } catch (error) {
      console.error('Error completing order:', error);
      // Handle error - show error message
    } finally {
      this.isProcessing.set(false);
    }
  }

  getNextPaymentDate(): Date {
    const schedule = this.getPaymentSchedule();
    return schedule.length > 0 ? schedule[0].date : new Date();
  }
}