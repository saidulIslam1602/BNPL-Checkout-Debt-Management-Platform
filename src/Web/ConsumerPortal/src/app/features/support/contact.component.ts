import { Component, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatSnackBarModule, MatSnackBar } from '@angular/material/snack-bar';

@Component({
  selector: 'app-contact',
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
    MatSnackBarModule
  ],
  template: `
    <div class="contact-container">
      <div class="hero-section">
        <h1>Contact Support</h1>
        <p class="subtitle">We're here to help with any questions or concerns</p>
      </div>

      <div class="contact-section">
        <div class="contact-methods">
          <mat-card class="contact-method">
            <mat-card-header>
              <mat-icon mat-card-avatar>phone</mat-icon>
              <mat-card-title>Phone Support</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <p><strong>+47 800 12 345</strong></p>
              <p>Monday - Friday: 8:00 - 20:00</p>
              <p>Saturday: 10:00 - 16:00</p>
            </mat-card-content>
          </mat-card>

          <mat-card class="contact-method">
            <mat-card-header>
              <mat-icon mat-card-avatar>email</mat-icon>
              <mat-card-title>Email Support</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <p><strong>support&#64;yourcompanybnpl.no</strong></p>
              <p>Response within 24 hours</p>
              <p>Available 24/7</p>
            </mat-card-content>
          </mat-card>

          <mat-card class="contact-method">
            <mat-card-header>
              <mat-icon mat-card-avatar>chat</mat-icon>
              <mat-card-title>Live Chat</mat-card-title>
            </mat-card-header>
            <mat-card-content>
              <p><strong>Available now</strong></p>
              <p>Monday - Friday: 8:00 - 20:00</p>
              <button mat-raised-button color="primary" (click)="startChat()">
                Start Chat
              </button>
            </mat-card-content>
          </mat-card>
        </div>

        <mat-card class="contact-form-card">
          <mat-card-header>
            <mat-card-title>Send us a message</mat-card-title>
          </mat-card-header>
          <mat-card-content>
            <form [formGroup]="contactForm" (ngSubmit)="onSubmit()">
              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Your Name</mat-label>
                <input matInput formControlName="name" required>
                <mat-error *ngIf="contactForm.get('name')?.hasError('required')">
                  Name is required
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Email Address</mat-label>
                <input matInput type="email" formControlName="email" required>
                <mat-error *ngIf="contactForm.get('email')?.hasError('required')">
                  Email is required
                </mat-error>
                <mat-error *ngIf="contactForm.get('email')?.hasError('email')">
                  Please enter a valid email
                </mat-error>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Subject</mat-label>
                <mat-select formControlName="subject" required>
                  @for (subject of subjects(); track subject.value) {
                    <mat-option [value]="subject.value">{{subject.label}}</mat-option>
                  }
                </mat-select>
              </mat-form-field>

              <mat-form-field appearance="outline" class="full-width">
                <mat-label>Message</mat-label>
                <textarea matInput rows="5" formControlName="message" required></textarea>
                <mat-error *ngIf="contactForm.get('message')?.hasError('required')">
                  Message is required
                </mat-error>
              </mat-form-field>

              <div class="form-actions">
                <button mat-raised-button color="primary" type="submit" [disabled]="contactForm.invalid || submitting()">
                  @if (submitting()) {
                    <ng-container>
                      <mat-icon>hourglass_empty</mat-icon>
                      Sending...
                    </ng-container>
                  } @else {
                    Send Message
                  }
                </button>
                <button mat-button type="button" (click)="resetForm()">Reset</button>
              </div>
            </form>
          </mat-card-content>
        </mat-card>
      </div>
    </div>
  `,
  styles: [`
    .contact-container {
      padding: 2rem;
      max-width: 1000px;
      margin: 0 auto;
    }

    .hero-section {
      text-align: center;
      margin-bottom: 3rem;
      
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

    .contact-methods {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
      gap: 1.5rem;
      margin-bottom: 2rem;
    }

    .contact-method {
      text-align: center;
      
      mat-icon[mat-card-avatar] {
        background: #1976d2;
        color: white;
      }
      
      p {
        margin: 0.5rem 0;
        
        &:first-child {
          font-size: 1.1rem;
        }
      }
    }

    .contact-form-card {
      margin-top: 2rem;
    }

    .full-width {
      width: 100%;
      margin-bottom: 1rem;
    }

    .form-actions {
      display: flex;
      gap: 1rem;
      justify-content: flex-end;
      margin-top: 1rem;
    }
  `]
})
export class ContactComponent {
  contactForm: FormGroup;
  submitting = signal(false);

  subjects = signal([
    { value: 'payment', label: 'Payment Issue' },
    { value: 'account', label: 'Account Question' },
    { value: 'technical', label: 'Technical Support' },
    { value: 'merchant', label: 'Merchant Issue' },
    { value: 'other', label: 'Other' }
  ]);

  constructor(
    private fb: FormBuilder,
    private snackBar: MatSnackBar
  ) {
    this.contactForm = this.fb.group({
      name: ['', Validators.required],
      email: ['', [Validators.required, Validators.email]],
      subject: ['', Validators.required],
      message: ['', Validators.required]
    });
  }

  onSubmit() {
    if (this.contactForm.valid) {
      this.submitting.set(true);
      
      // TODO: Submit to API
      setTimeout(() => {
        this.submitting.set(false);
        this.snackBar.open('Message sent successfully!', 'Close', { duration: 3000 });
        this.resetForm();
      }, 2000);
    }
  }

  resetForm() {
    this.contactForm.reset();
  }

  startChat() {
    // TODO: Initialize chat widget
    console.log('Starting chat...');
  }
}