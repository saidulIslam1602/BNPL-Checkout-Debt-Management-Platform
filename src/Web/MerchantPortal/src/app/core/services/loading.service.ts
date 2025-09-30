import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class LoadingService {
  private loadingSubject = new BehaviorSubject<boolean>(false);
  private loadingCountSubject = new BehaviorSubject<number>(0);
  
  public loading$ = this.loadingSubject.asObservable();
  public loadingCount$ = this.loadingCountSubject.asObservable();

  private loadingMap = new Map<string, boolean>();

  /**
   * Show loading indicator
   */
  show(key?: string): void {
    if (key) {
      this.loadingMap.set(key, true);
    }
    
    const currentCount = this.loadingCountSubject.value;
    this.loadingCountSubject.next(currentCount + 1);
    this.loadingSubject.next(true);
  }

  /**
   * Hide loading indicator
   */
  hide(key?: string): void {
    if (key) {
      this.loadingMap.delete(key);
    }
    
    const currentCount = this.loadingCountSubject.value;
    const newCount = Math.max(0, currentCount - 1);
    this.loadingCountSubject.next(newCount);
    
    if (newCount === 0) {
      this.loadingSubject.next(false);
    }
  }

  /**
   * Check if a specific loading key is active
   */
  isLoading(key?: string): boolean {
    if (key) {
      return this.loadingMap.has(key);
    }
    return this.loadingSubject.value;
  }

  /**
   * Clear all loading states
   */
  clear(): void {
    this.loadingMap.clear();
    this.loadingCountSubject.next(0);
    this.loadingSubject.next(false);
  }
}