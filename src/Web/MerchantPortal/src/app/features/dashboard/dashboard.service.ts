import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class DashboardService {
  
  getDashboardData(): Observable<any> {
    const mockData = {
      totalSales: 1234567,
      totalOrders: 456,
      conversionRate: 3.2,
      averageOrder: 2850
    };
    
    return of(mockData).pipe(delay(500));
  }

  getRecentTransactions(): Observable<any[]> {
    const mockTransactions = [
      { id: '1', orderId: 'ORD-001', amount: 15000, status: 'completed' },
      { id: '2', orderId: 'ORD-002', amount: 8500, status: 'pending' }
    ];
    
    return of(mockTransactions).pipe(delay(300));
  }
}