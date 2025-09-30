import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  
  getTransactions(): Observable<any[]> {
    const mockTransactions = [
      {
        id: '1',
        orderId: 'ORD-12345678',
        customerName: 'John Doe',
        amount: 15000,
        status: 'completed',
        createdAt: new Date(Date.now() - 24 * 60 * 60 * 1000)
      },
      {
        id: '2',
        orderId: 'ORD-87654321',
        customerName: 'Jane Smith',
        amount: 8500,
        status: 'pending',
        createdAt: new Date(Date.now() - 2 * 60 * 60 * 1000)
      }
    ];
    
    return of(mockTransactions).pipe(delay(500));
  }

  getTransaction(id: string): Observable<any> {
    const mockTransaction = {
      id,
      orderId: 'ORD-12345678',
      customerName: 'John Doe',
      amount: 15000,
      status: 'completed',
      planType: 'Pay in 4',
      totalInstallments: 4,
      paidInstallments: 4,
      nextPaymentDate: new Date(),
      createdAt: new Date(Date.now() - 24 * 60 * 60 * 1000)
    };
    
    return of(mockTransaction).pipe(delay(300));
  }
}