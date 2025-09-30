import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, of, catchError } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface Transaction {
  id: string;
  customerId: string;
  customerName: string;
  customerEmail: string;
  amount: number;
  currency: string;
  status: 'pending' | 'processing' | 'completed' | 'failed' | 'cancelled' | 'refunded';
  paymentMethod: string;
  paymentProvider: string;
  bnplPlan?: BNPLPlan;
  merchantReference?: string;
  description?: string;
  createdAt: Date;
  updatedAt: Date;
  completedAt?: Date;
  riskLevel: 'low' | 'medium' | 'high';
  riskScore: number;
  fraudScore: number;
  location?: TransactionLocation;
  metadata?: { [key: string]: any };
  refunds?: TransactionRefund[];
  fees?: TransactionFee[];
}

export interface BNPLPlan {
  id: string;
  type: 'pay_in_3' | 'pay_in_4' | 'pay_in_6' | 'pay_in_12' | 'pay_in_24';
  installments: BNPLInstallment[];
  totalAmount: number;
  interestRate: number;
  fees: number;
  status: 'active' | 'completed' | 'defaulted' | 'cancelled';
}

export interface BNPLInstallment {
  id: string;
  amount: number;
  dueDate: Date;
  status: 'pending' | 'paid' | 'overdue' | 'failed';
  paidAt?: Date;
  lateFee?: number;
}

export interface TransactionLocation {
  country: string;
  city?: string;
  ipAddress: string;
  coordinates?: { lat: number; lng: number };
}

export interface TransactionRefund {
  id: string;
  amount: number;
  reason: string;
  status: 'pending' | 'completed' | 'failed';
  createdAt: Date;
  processedAt?: Date;
}

export interface TransactionFee {
  type: 'processing' | 'late' | 'currency_conversion' | 'risk_adjustment';
  amount: number;
  description: string;
}

export interface TransactionFilter {
  status?: string[];
  paymentMethod?: string[];
  riskLevel?: string[];
  dateFrom?: Date;
  dateTo?: Date;
  amountMin?: number;
  amountMax?: number;
  customerId?: string;
  merchantReference?: string;
  search?: string;
}

export interface TransactionSummary {
  totalCount: number;
  totalAmount: number;
  successfulCount: number;
  failedCount: number;
  pendingCount: number;
  refundedAmount: number;
  averageAmount: number;
  successRate: number;
}

export interface RealtimeTransaction {
  id: string;
  customerId: string;
  customerName: string;
  amount: number;
  currency: string;
  status: 'pending' | 'completed' | 'failed' | 'processing';
  paymentMethod: string;
  bnplPlan?: string;
  timestamp: Date;
  riskLevel: 'low' | 'medium' | 'high';
  location?: string;
  merchantReference?: string;
}

@Injectable({
  providedIn: 'root'
})
export class TransactionService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/transactions`;

  // Real-time transaction cache
  private realtimeTransactionsSubject = new BehaviorSubject<RealtimeTransaction[]>([]);
  public realtimeTransactions$ = this.realtimeTransactionsSubject.asObservable();

  /**
   * Get paginated transactions with filtering
   */
  getTransactions(
    page: number = 1, 
    pageSize: number = 20, 
    filter?: TransactionFilter,
    sortBy: string = 'createdAt',
    sortDirection: 'asc' | 'desc' = 'desc'
  ): Observable<{ transactions: Transaction[]; total: number; summary: TransactionSummary }> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString())
      .set('sortBy', sortBy)
      .set('sortDirection', sortDirection);

    if (filter) {
      if (filter.status?.length) {
        params = params.set('status', filter.status.join(','));
      }
      if (filter.paymentMethod?.length) {
        params = params.set('paymentMethod', filter.paymentMethod.join(','));
      }
      if (filter.riskLevel?.length) {
        params = params.set('riskLevel', filter.riskLevel.join(','));
      }
      if (filter.dateFrom) {
        params = params.set('dateFrom', filter.dateFrom.toISOString());
      }
      if (filter.dateTo) {
        params = params.set('dateTo', filter.dateTo.toISOString());
      }
      if (filter.amountMin !== undefined) {
        params = params.set('amountMin', filter.amountMin.toString());
      }
      if (filter.amountMax !== undefined) {
        params = params.set('amountMax', filter.amountMax.toString());
      }
      if (filter.customerId) {
        params = params.set('customerId', filter.customerId);
      }
      if (filter.merchantReference) {
        params = params.set('merchantReference', filter.merchantReference);
      }
      if (filter.search) {
        params = params.set('search', filter.search);
      }
    }

    return this.http.get<{ transactions: Transaction[]; total: number; summary: TransactionSummary }>(
      this.baseUrl, { params }
    ).pipe(
      catchError(error => {
        console.error('Error fetching transactions:', error);
        return of(this.getMockTransactionsResponse());
      })
    );
  }

  /**
   * Get a single transaction by ID
   */
  getTransaction(id: string): Observable<Transaction> {
    return this.http.get<Transaction>(`${this.baseUrl}/${id}`).pipe(
      catchError(error => {
        console.error('Error fetching transaction:', error);
        return of(this.getMockTransaction(id));
      })
    );
  }

  /**
   * Get real-time transactions for dashboard
   */
  getRealtimeTransactions(): Observable<RealtimeTransaction[]> {
    return this.http.get<RealtimeTransaction[]>(`${this.baseUrl}/realtime`).pipe(
      catchError(error => {
        console.error('Error fetching realtime transactions:', error);
        return of(this.getMockRealtimeTransactions());
      })
    );
  }

  /**
   * Process a pending transaction
   */
  processTransaction(id: string): Observable<Transaction> {
    return this.http.post<Transaction>(`${this.baseUrl}/${id}/process`, {}).pipe(
      catchError(error => {
        console.error('Error processing transaction:', error);
        throw error;
      })
    );
  }

  /**
   * Cancel a transaction
   */
  cancelTransaction(id: string, reason: string): Observable<Transaction> {
    return this.http.post<Transaction>(`${this.baseUrl}/${id}/cancel`, { reason }).pipe(
      catchError(error => {
        console.error('Error cancelling transaction:', error);
        throw error;
      })
    );
  }

  /**
   * Refund a transaction
   */
  refundTransaction(id: string, amount: number, reason: string): Observable<TransactionRefund> {
    return this.http.post<TransactionRefund>(`${this.baseUrl}/${id}/refund`, { 
      amount, 
      reason 
    }).pipe(
      catchError(error => {
        console.error('Error refunding transaction:', error);
        throw error;
      })
    );
  }

  /**
   * Get transaction statistics
   */
  getTransactionStats(period: 'day' | 'week' | 'month' | 'year' = 'month'): Observable<any> {
    const params = new HttpParams().set('period', period);
    
    return this.http.get(`${this.baseUrl}/stats`, { params }).pipe(
      catchError(error => {
        console.error('Error fetching transaction stats:', error);
        return of(this.getMockTransactionStats());
      })
    );
  }

  /**
   * Export transactions
   */
  exportTransactions(
    filter?: TransactionFilter, 
    format: 'csv' | 'excel' | 'pdf' = 'csv'
  ): Observable<Blob> {
    let params = new HttpParams().set('format', format);

    if (filter) {
      // Add filter parameters similar to getTransactions
      if (filter.status?.length) {
        params = params.set('status', filter.status.join(','));
      }
      // ... add other filter parameters
    }

    return this.http.get(`${this.baseUrl}/export`, { 
      params, 
      responseType: 'blob' 
    });
  }

  /**
   * Get BNPL plan details
   */
  getBNPLPlan(planId: string): Observable<BNPLPlan> {
    return this.http.get<BNPLPlan>(`${this.baseUrl}/bnpl-plans/${planId}`).pipe(
      catchError(error => {
        console.error('Error fetching BNPL plan:', error);
        return of(this.getMockBNPLPlan(planId));
      })
    );
  }

  /**
   * Update BNPL installment
   */
  updateBNPLInstallment(planId: string, installmentId: string, data: Partial<BNPLInstallment>): Observable<BNPLInstallment> {
    return this.http.patch<BNPLInstallment>(
      `${this.baseUrl}/bnpl-plans/${planId}/installments/${installmentId}`, 
      data
    ).pipe(
      catchError(error => {
        console.error('Error updating BNPL installment:', error);
        throw error;
      })
    );
  }

  // Mock data methods for development
  private getMockTransactionsResponse(): { transactions: Transaction[]; total: number; summary: TransactionSummary } {
    const transactions = Array.from({ length: 20 }, (_, i) => this.getMockTransaction(`tx_${i + 1}`));
    
    return {
      transactions,
      total: 1247,
      summary: {
        totalCount: 1247,
        totalAmount: 2450000,
        successfulCount: 1198,
        failedCount: 32,
        pendingCount: 17,
        refundedAmount: 45000,
        averageAmount: 1850,
        successRate: 96.1
      }
    };
  }

  private getMockTransaction(id: string): Transaction {
    const statuses: Transaction['status'][] = ['pending', 'processing', 'completed', 'failed'];
    const paymentMethods = ['Vipps', 'Card', 'Bank Transfer', 'BNPL'];
    const riskLevels: Transaction['riskLevel'][] = ['low', 'medium', 'high'];
    
    return {
      id,
      customerId: `cust_${Math.random().toString(36).substr(2, 9)}`,
      customerName: `Customer ${id}`,
      customerEmail: `customer${id}@example.com`,
      amount: Math.floor(Math.random() * 5000) + 500,
      currency: 'NOK',
      status: statuses[Math.floor(Math.random() * statuses.length)],
      paymentMethod: paymentMethods[Math.floor(Math.random() * paymentMethods.length)],
      paymentProvider: 'Adyen',
      merchantReference: `REF_${Math.random().toString(36).substr(2, 9).toUpperCase()}`,
      description: `Purchase from Norwegian merchant`,
      createdAt: new Date(Date.now() - Math.random() * 7 * 24 * 60 * 60 * 1000),
      updatedAt: new Date(),
      riskLevel: riskLevels[Math.floor(Math.random() * riskLevels.length)],
      riskScore: Math.floor(Math.random() * 100),
      fraudScore: Math.floor(Math.random() * 50),
      location: {
        country: 'Norway',
        city: ['Oslo', 'Bergen', 'Trondheim', 'Stavanger'][Math.floor(Math.random() * 4)],
        ipAddress: `192.168.${Math.floor(Math.random() * 255)}.${Math.floor(Math.random() * 255)}`
      }
    };
  }

  private getMockRealtimeTransactions(): RealtimeTransaction[] {
    return Array.from({ length: 8 }, (_, i) => ({
      id: `rt_${i + 1}`,
      customerId: `cust_${Math.random().toString(36).substr(2, 9)}`,
      customerName: `Customer ${i + 1}`,
      amount: Math.floor(Math.random() * 3000) + 500,
      currency: 'NOK',
      status: ['pending', 'completed', 'failed', 'processing'][Math.floor(Math.random() * 4)] as any,
      paymentMethod: ['Vipps', 'Card', 'Bank Transfer', 'BNPL'][Math.floor(Math.random() * 4)],
      bnplPlan: Math.random() > 0.6 ? 'Pay in 3' : undefined,
      timestamp: new Date(Date.now() - Math.random() * 60 * 60 * 1000),
      riskLevel: ['low', 'medium', 'high'][Math.floor(Math.random() * 3)] as any,
      location: ['Oslo', 'Bergen', 'Trondheim'][Math.floor(Math.random() * 3)],
      merchantReference: `REF_${Math.random().toString(36).substr(2, 6).toUpperCase()}`
    }));
  }

  private getMockBNPLPlan(id: string): BNPLPlan {
    return {
      id,
      type: 'pay_in_3',
      installments: [
        {
          id: 'inst_1',
          amount: 500,
          dueDate: new Date(Date.now() + 30 * 24 * 60 * 60 * 1000),
          status: 'pending'
        },
        {
          id: 'inst_2',
          amount: 500,
          dueDate: new Date(Date.now() + 60 * 24 * 60 * 60 * 1000),
          status: 'pending'
        },
        {
          id: 'inst_3',
          amount: 500,
          dueDate: new Date(Date.now() + 90 * 24 * 60 * 60 * 1000),
          status: 'pending'
        }
      ],
      totalAmount: 1500,
      interestRate: 0,
      fees: 0,
      status: 'active'
    };
  }

  private getMockTransactionStats(): any {
    return {
      totalTransactions: 15420,
      totalAmount: 28500000,
      averageAmount: 1850,
      successRate: 96.5,
      topPaymentMethods: [
        { method: 'Vipps', count: 6168, percentage: 40 },
        { method: 'Card', count: 4626, percentage: 30 },
        { method: 'BNPL', count: 3084, percentage: 20 },
        { method: 'Bank Transfer', count: 1542, percentage: 10 }
      ],
      dailyTrends: Array.from({ length: 30 }, (_, i) => ({
        date: new Date(Date.now() - (29 - i) * 24 * 60 * 60 * 1000).toISOString().split('T')[0],
        count: Math.floor(Math.random() * 200) + 300,
        amount: Math.floor(Math.random() * 500000) + 800000
      }))
    };
  }
}