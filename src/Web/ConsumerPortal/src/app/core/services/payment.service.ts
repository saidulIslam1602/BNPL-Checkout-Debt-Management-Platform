import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { Payment, PaymentStatus, BNPLPaymentResponse } from '../models/payment.model';

export interface BNPLPaymentRequest {
  customer: any;
  items: any[];
  bnplOption: any;
  total: number;
  terms: any;
  sessionId?: string;
}

// Interfaces moved to ../models/payment.model.ts

@Injectable({
  providedIn: 'root'
})
export class PaymentService {
  private http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/payments`;

  /**
   * Create BNPL payment
   */
  createBNPLPayment(paymentRequest: BNPLPaymentRequest): Observable<BNPLPaymentResponse> {
    if (environment.features.mockData) {
      // Simulate payment processing
      const orderNumber = 'ORD-' + Date.now().toString().slice(-8);
      const paymentId = 'PAY-' + Math.random().toString(36).substr(2, 9).toUpperCase();
      
      return of({
        success: true,
        orderNumber,
        paymentId,
        status: 'approved' as const,
        nextPaymentDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000) // 30 days from now
      }).pipe(delay(2000)); // Simulate processing time
    }

    return this.http.post<BNPLPaymentResponse>(`${this.baseUrl}/bnpl`, paymentRequest);
  }

  /**
   * Get user's payments
   */
  getMyPayments(): Observable<Payment[]> {
    if (environment.features.mockData) {
      const mockPayments: Payment[] = [
        {
          id: 'PAY-001',
          orderNumber: 'ORD-12345678',
          amount: 15000,
          currency: 'NOK',
          status: 'completed',
          totalInstallments: 4,
          paidInstallments: 4,
          nextPaymentDate: new Date(),
          createdAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000),
          updatedAt: new Date()
        },
        {
          id: 'PAY-002',
          orderNumber: 'ORD-87654321',
          amount: 8500,
          currency: 'NOK',
          status: 'pending',
          totalInstallments: 3,
          paidInstallments: 1,
          nextPaymentDate: new Date(Date.now() + 7 * 24 * 60 * 60 * 1000),
          createdAt: new Date(Date.now() - 15 * 24 * 60 * 60 * 1000),
          updatedAt: new Date()
        }
      ];
      
      return of(mockPayments).pipe(delay(1000));
    }

    return this.http.get<Payment[]>(`${this.baseUrl}/my-payments`);
  }

  /**
   * Get payment status
   */
  getPaymentStatus(paymentId: string): Observable<PaymentStatus> {
    if (environment.features.mockData) {
      return of({
        paymentId,
        status: 'completed' as const,
        amount: 16980,
        currency: 'NOK',
        createdAt: new Date(Date.now() - 60000), // 1 minute ago
        updatedAt: new Date()
      }).pipe(delay(500));
    }

    return this.http.get<PaymentStatus>(`${this.baseUrl}/${paymentId}/status`);
  }

  /**
   * Get customer payments
   */
  getCustomerPayments(customerId: string): Observable<any[]> {
    if (environment.features.mockData) {
      return of([
        {
          id: 'PAY-ABC123',
          orderNumber: 'ORD-12345678',
          merchantName: 'Elkjøp',
          amount: 16980,
          currency: 'NOK',
          status: 'active',
          createdAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000), // 7 days ago
          nextPaymentDate: new Date(Date.now() + 23 * 24 * 60 * 60 * 1000), // 23 days from now
          nextPaymentAmount: 5660,
          remainingAmount: 11320,
          installmentsRemaining: 2,
          totalInstallments: 3,
          items: [
            {
              name: 'iPhone 15 Pro 128GB',
              imageUrl: 'assets/images/iphone-15-pro.jpg'
            }
          ]
        },
        {
          id: 'PAY-DEF456',
          orderNumber: 'ORD-87654321',
          merchantName: 'Komplett',
          amount: 8990,
          currency: 'NOK',
          status: 'completed',
          createdAt: new Date(Date.now() - 90 * 24 * 60 * 60 * 1000), // 90 days ago
          completedAt: new Date(Date.now() - 30 * 24 * 60 * 60 * 1000), // 30 days ago
          totalInstallments: 4,
          items: [
            {
              name: 'Gaming Headset',
              imageUrl: 'assets/images/gaming-headset.jpg'
            }
          ]
        }
      ]).pipe(delay(800));
    }

    return this.http.get<any[]>(`${this.baseUrl}/customer/${customerId}`);
  }

  /**
   * Get payment details
   */
  getPaymentDetails(paymentId: string): Observable<any> {
    if (environment.features.mockData) {
      return of({
        id: paymentId,
        orderNumber: 'ORD-12345678',
        merchantName: 'Elkjøp',
        merchantLogo: 'assets/images/merchants/elkjop-logo.svg',
        amount: 16980,
        currency: 'NOK',
        status: 'active',
        createdAt: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
        bnplOption: {
          type: 'PAY_IN_3',
          displayName: 'Betal i 3 deler',
          interestRate: 0
        },
        customer: {
          firstName: 'Ola',
          lastName: 'Nordmann',
          email: 'ola.nordmann@example.no'
        },
        items: [
          {
            id: '1',
            name: 'iPhone 15 Pro 128GB',
            description: 'Titanium Natural',
            price: 13990,
            quantity: 1,
            imageUrl: 'assets/images/iphone-15-pro.jpg'
          },
          {
            id: '2',
            name: 'AirPods Pro (2nd generation)',
            description: 'Med MagSafe Charging Case',
            price: 2990,
            quantity: 1,
            imageUrl: 'assets/images/airpods-pro.jpg'
          }
        ],
        installments: [
          {
            installmentNumber: 1,
            amount: 5660,
            dueDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000),
            status: 'paid',
            paidDate: new Date(Date.now() - 7 * 24 * 60 * 60 * 1000)
          },
          {
            installmentNumber: 2,
            amount: 5660,
            dueDate: new Date(Date.now() + 23 * 24 * 60 * 60 * 1000),
            status: 'pending'
          },
          {
            installmentNumber: 3,
            amount: 5660,
            dueDate: new Date(Date.now() + 53 * 24 * 60 * 60 * 1000),
            status: 'pending'
          }
        ]
      }).pipe(delay(600));
    }

    return this.http.get(`${this.baseUrl}/${paymentId}`);
  }

  /**
   * Make early payment
   */
  makeEarlyPayment(paymentId: string, amount: number): Observable<any> {
    if (environment.features.mockData) {
      return of({
        success: true,
        message: 'Tidlig betaling behandlet',
        newBalance: Math.max(0, 11320 - amount)
      }).pipe(delay(1500));
    }

    return this.http.post(`${this.baseUrl}/${paymentId}/early-payment`, { amount });
  }

  /**
   * Update payment method
   */
  updatePaymentMethod(paymentId: string, paymentMethod: any): Observable<any> {
    if (environment.features.mockData) {
      return of({
        success: true,
        message: 'Betalingsmetode oppdatert'
      }).pipe(delay(1000));
    }

    return this.http.patch(`${this.baseUrl}/${paymentId}/payment-method`, paymentMethod);
  }

  /**
   * Cancel payment plan
   */
  cancelPaymentPlan(paymentId: string, reason: string): Observable<any> {
    if (environment.features.mockData) {
      return of({
        success: true,
        message: 'Betalingsplan kansellert',
        cancellationFee: 0
      }).pipe(delay(1200));
    }

    return this.http.post(`${this.baseUrl}/${paymentId}/cancel`, { reason });
  }

  /**
   * Get payment statistics
   */
  getPaymentStatistics(customerId: string): Observable<any> {
    if (environment.features.mockData) {
      return of({
        totalPayments: 5,
        activePayments: 1,
        completedPayments: 4,
        totalAmountPaid: 45670,
        totalAmountRemaining: 11320,
        averagePaymentAmount: 9134,
        onTimePaymentRate: 0.95,
        creditScore: 720
      }).pipe(delay(700));
    }

    return this.http.get(`${this.baseUrl}/customer/${customerId}/statistics`);
  }
}