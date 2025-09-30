import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { delay } from 'rxjs/operators';

@Injectable({
  providedIn: 'root'
})
export class AnalyticsService {
  
  getAnalyticsData(): Observable<any> {
    const mockData = {
      revenue: {
        current: 1234567,
        previous: 1100000,
        growth: 12.2
      },
      orders: {
        current: 456,
        previous: 420,
        growth: 8.6
      },
      customers: {
        current: 234,
        previous: 210,
        growth: 11.4
      }
    };
    
    return of(mockData).pipe(delay(500));
  }

  getChartData(): Observable<any> {
    const mockChartData = {
      labels: ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun'],
      datasets: [
        {
          label: 'Sales',
          data: [120000, 150000, 180000, 220000, 190000, 250000],
          borderColor: '#1976d2',
          backgroundColor: 'rgba(25, 118, 210, 0.1)'
        }
      ]
    };
    
    return of(mockChartData).pipe(delay(300));
  }
}