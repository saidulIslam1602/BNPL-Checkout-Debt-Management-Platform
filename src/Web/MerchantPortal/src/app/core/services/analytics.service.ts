import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, interval, switchMap, catchError, of } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface DashboardAnalytics {
  totalRevenue: number;
  previousRevenue: number;
  activePlans: number;
  previousActivePlans: number;
  conversionRate: number;
  previousConversionRate: number;
  defaultRate: number;
  previousDefaultRate: number;
  averageOrderValue: number;
  previousAverageOrderValue: number;
  customerSatisfaction: number;
  previousCustomerSatisfaction: number;
  revenueTrend: number[];
  plansTrend: number[];
  conversionTrend: number[];
  defaultTrend: number[];
  aovTrend: number[];
  satisfactionTrend: number[];
}

export interface RevenueAnalytics {
  totalRevenue: number;
  monthlyRevenue: { month: string; revenue: number }[];
  revenueByPaymentMethod: { method: string; revenue: number; percentage: number }[];
  revenueByBNPLPlan: { plan: string; revenue: number; percentage: number }[];
  projectedRevenue: number;
  growthRate: number;
}

export interface CustomerAnalytics {
  totalCustomers: number;
  newCustomers: number;
  returningCustomers: number;
  customerLifetimeValue: number;
  customerAcquisitionCost: number;
  churnRate: number;
  customersByRiskLevel: { riskLevel: string; count: number; percentage: number }[];
  customerSatisfactionScore: number;
}

export interface TransactionAnalytics {
  totalTransactions: number;
  successfulTransactions: number;
  failedTransactions: number;
  successRate: number;
  averageTransactionValue: number;
  transactionsByHour: { hour: number; count: number }[];
  transactionsByDay: { day: string; count: number }[];
  paymentMethodDistribution: { method: string; count: number; percentage: number }[];
}

export interface RiskAnalytics {
  totalRiskAssessments: number;
  approvedAssessments: number;
  declinedAssessments: number;
  approvalRate: number;
  averageRiskScore: number;
  riskDistribution: { level: string; count: number; percentage: number }[];
  fraudDetections: number;
  fraudRate: number;
  topFraudRules: { rule: string; triggers: number }[];
}

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  private http = inject(HttpClient);
  private baseUrl = `${environment.apiUrl}/analytics`;

  // Real-time analytics cache
  private dashboardAnalyticsSubject = new BehaviorSubject<DashboardAnalytics | null>(null);
  public dashboardAnalytics$ = this.dashboardAnalyticsSubject.asObservable();

  constructor() {
    // Set up real-time updates every 2 minutes
    interval(120000).pipe(
      switchMap(() => this.fetchDashboardAnalytics()),
      catchError(error => {
        console.error('Error fetching real-time analytics:', error);
        return of(null);
      })
    ).subscribe(data => {
      if (data) {
        this.dashboardAnalyticsSubject.next(data);
      }
    });
  }

  /**
   * Get dashboard analytics with caching
   */
  getDashboardAnalytics(): Observable<DashboardAnalytics> {
    // Return cached data if available, otherwise fetch fresh data
    const cachedData = this.dashboardAnalyticsSubject.value;
    if (cachedData) {
      return of(cachedData);
    }
    
    return this.fetchDashboardAnalytics();
  }

  /**
   * Fetch fresh dashboard analytics from API
   */
  private fetchDashboardAnalytics(): Observable<DashboardAnalytics> {
    return this.http.get<DashboardAnalytics>(`${this.baseUrl}/dashboard`).pipe(
      catchError(error => {
        console.error('Error fetching dashboard analytics:', error);
        // Return mock data for development
        return of(this.getMockDashboardAnalytics());
      })
    );
  }

  /**
   * Get revenue analytics for a specific period
   */
  getRevenueAnalytics(startDate: Date, endDate: Date): Observable<RevenueAnalytics> {
    const params = new HttpParams()
      .set('startDate', startDate.toISOString())
      .set('endDate', endDate.toISOString());

    return this.http.get<RevenueAnalytics>(`${this.baseUrl}/revenue`, { params }).pipe(
      catchError(error => {
        console.error('Error fetching revenue analytics:', error);
        return of(this.getMockRevenueAnalytics());
      })
    );
  }

  /**
   * Get customer analytics
   */
  getCustomerAnalytics(period: 'day' | 'week' | 'month' | 'year' = 'month'): Observable<CustomerAnalytics> {
    const params = new HttpParams().set('period', period);

    return this.http.get<CustomerAnalytics>(`${this.baseUrl}/customers`, { params }).pipe(
      catchError(error => {
        console.error('Error fetching customer analytics:', error);
        return of(this.getMockCustomerAnalytics());
      })
    );
  }

  /**
   * Get transaction analytics
   */
  getTransactionAnalytics(period: 'day' | 'week' | 'month' | 'year' = 'month'): Observable<TransactionAnalytics> {
    const params = new HttpParams().set('period', period);

    return this.http.get<TransactionAnalytics>(`${this.baseUrl}/transactions`, { params }).pipe(
      catchError(error => {
        console.error('Error fetching transaction analytics:', error);
        return of(this.getMockTransactionAnalytics());
      })
    );
  }

  /**
   * Get risk analytics
   */
  getRiskAnalytics(period: 'day' | 'week' | 'month' | 'year' = 'month'): Observable<RiskAnalytics> {
    const params = new HttpParams().set('period', period);

    return this.http.get<RiskAnalytics>(`${this.baseUrl}/risk`, { params }).pipe(
      catchError(error => {
        console.error('Error fetching risk analytics:', error);
        return of(this.getMockRiskAnalytics());
      })
    );
  }

  /**
   * Get Norwegian market benchmarks
   */
  getNorwegianMarketBenchmarks(): Observable<any> {
    return this.http.get(`${this.baseUrl}/norwegian-benchmarks`).pipe(
      catchError(error => {
        console.error('Error fetching Norwegian market benchmarks:', error);
        return of(this.getMockNorwegianBenchmarks());
      })
    );
  }

  /**
   * Export analytics data
   */
  exportAnalytics(type: 'revenue' | 'customers' | 'transactions' | 'risk', format: 'csv' | 'excel' | 'pdf'): Observable<Blob> {
    const params = new HttpParams()
      .set('type', type)
      .set('format', format);

    return this.http.get(`${this.baseUrl}/export`, { 
      params, 
      responseType: 'blob' 
    });
  }

  // Mock data methods for development
  private getMockDashboardAnalytics(): DashboardAnalytics {
    return {
      totalRevenue: 2450000,
      previousRevenue: 2180000,
      activePlans: 1247,
      previousActivePlans: 1156,
      conversionRate: 68.5,
      previousConversionRate: 65.2,
      defaultRate: 2.8,
      previousDefaultRate: 3.1,
      averageOrderValue: 1850,
      previousAverageOrderValue: 1720,
      customerSatisfaction: 87.5,
      previousCustomerSatisfaction: 85.2,
      revenueTrend: [2100000, 2200000, 2180000, 2300000, 2350000, 2450000],
      plansTrend: [1050, 1100, 1156, 1180, 1220, 1247],
      conversionTrend: [62.1, 63.5, 65.2, 66.8, 67.9, 68.5],
      defaultTrend: [3.5, 3.3, 3.1, 3.0, 2.9, 2.8],
      aovTrend: [1650, 1680, 1720, 1780, 1820, 1850],
      satisfactionTrend: [82.1, 83.5, 85.2, 86.1, 86.8, 87.5]
    };
  }

  private getMockRevenueAnalytics(): RevenueAnalytics {
    return {
      totalRevenue: 2450000,
      monthlyRevenue: [
        { month: 'Jan', revenue: 2100000 },
        { month: 'Feb', revenue: 2200000 },
        { month: 'Mar', revenue: 2180000 },
        { month: 'Apr', revenue: 2300000 },
        { month: 'May', revenue: 2350000 },
        { month: 'Jun', revenue: 2450000 }
      ],
      revenueByPaymentMethod: [
        { method: 'Vipps', revenue: 980000, percentage: 40 },
        { method: 'Card', revenue: 735000, percentage: 30 },
        { method: 'BNPL', revenue: 490000, percentage: 20 },
        { method: 'Bank Transfer', revenue: 245000, percentage: 10 }
      ],
      revenueByBNPLPlan: [
        { plan: 'Pay in 3', revenue: 294000, percentage: 60 },
        { plan: 'Pay in 4', revenue: 147000, percentage: 30 },
        { plan: 'Pay in 6', revenue: 49000, percentage: 10 }
      ],
      projectedRevenue: 2650000,
      growthRate: 12.4
    };
  }

  private getMockCustomerAnalytics(): CustomerAnalytics {
    return {
      totalCustomers: 8450,
      newCustomers: 342,
      returningCustomers: 1156,
      customerLifetimeValue: 4250,
      customerAcquisitionCost: 125,
      churnRate: 5.2,
      customersByRiskLevel: [
        { riskLevel: 'Low', count: 5915, percentage: 70 },
        { riskLevel: 'Medium', count: 2028, percentage: 24 },
        { riskLevel: 'High', count: 507, percentage: 6 }
      ],
      customerSatisfactionScore: 87.5
    };
  }

  private getMockTransactionAnalytics(): TransactionAnalytics {
    return {
      totalTransactions: 15420,
      successfulTransactions: 14876,
      failedTransactions: 544,
      successRate: 96.5,
      averageTransactionValue: 1850,
      transactionsByHour: Array.from({ length: 24 }, (_, i) => ({
        hour: i,
        count: Math.floor(Math.random() * 100) + 50
      })),
      transactionsByDay: [
        { day: 'Monday', count: 2200 },
        { day: 'Tuesday', count: 2100 },
        { day: 'Wednesday', count: 2300 },
        { day: 'Thursday', count: 2400 },
        { day: 'Friday', count: 2800 },
        { day: 'Saturday', count: 2020 },
        { day: 'Sunday', count: 1600 }
      ],
      paymentMethodDistribution: [
        { method: 'Vipps', count: 6168, percentage: 40 },
        { method: 'Card', count: 4626, percentage: 30 },
        { method: 'BNPL', count: 3084, percentage: 20 },
        { method: 'Bank Transfer', count: 1542, percentage: 10 }
      ]
    };
  }

  private getMockRiskAnalytics(): RiskAnalytics {
    return {
      totalRiskAssessments: 12450,
      approvedAssessments: 10825,
      declinedAssessments: 1625,
      approvalRate: 86.9,
      averageRiskScore: 72.5,
      riskDistribution: [
        { level: 'Low', count: 8715, percentage: 70 },
        { level: 'Medium', count: 2988, percentage: 24 },
        { level: 'High', count: 747, percentage: 6 }
      ],
      fraudDetections: 156,
      fraudRate: 1.25,
      topFraudRules: [
        { rule: 'High Velocity', triggers: 45 },
        { rule: 'Suspicious IP', triggers: 38 },
        { rule: 'Device Risk', triggers: 32 },
        { rule: 'Amount Anomaly', triggers: 25 },
        { rule: 'Location Risk', triggers: 16 }
      ]
    };
  }

  private getMockNorwegianBenchmarks(): any {
    return {
      averageConversionRate: 65.8,
      averageDefaultRate: 3.2,
      averageOrderValue: 1750,
      popularPaymentMethods: ['Vipps', 'Card', 'BNPL'],
      seasonalTrends: {
        blackFriday: { increase: 45 },
        christmas: { increase: 38 },
        summer: { decrease: 15 }
      }
    };
  }
}