import { Injectable, inject } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';
import { BehaviorSubject, Observable } from 'rxjs';

export interface Notification {
  id: string;
  type: 'success' | 'error' | 'warning' | 'info';
  title: string;
  message: string;
  timestamp: Date;
  read: boolean;
  actions?: NotificationAction[];
  data?: any;
}

export interface NotificationAction {
  label: string;
  action: () => void;
  color?: 'primary' | 'accent' | 'warn';
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private snackBar = inject(MatSnackBar);
  
  private notificationsSubject = new BehaviorSubject<Notification[]>([]);
  public notifications$ = this.notificationsSubject.asObservable();

  private unreadCountSubject = new BehaviorSubject<number>(0);
  public unreadCount$ = this.unreadCountSubject.asObservable();

  /**
   * Show success notification
   */
  showSuccess(message: string, title: string = 'Success', duration: number = 5000): void {
    this.showSnackBar(message, 'success', duration);
    this.addNotification('success', title, message);
  }

  /**
   * Show error notification
   */
  showError(message: string, title: string = 'Error', duration: number = 8000): void {
    this.showSnackBar(message, 'error', duration);
    this.addNotification('error', title, message);
  }

  /**
   * Show warning notification
   */
  showWarning(message: string, title: string = 'Warning', duration: number = 6000): void {
    this.showSnackBar(message, 'warning', duration);
    this.addNotification('warning', title, message);
  }

  /**
   * Show info notification
   */
  showInfo(message: string, title: string = 'Information', duration: number = 5000): void {
    this.showSnackBar(message, 'info', duration);
    this.addNotification('info', title, message);
  }

  /**
   * Show custom notification with actions
   */
  showCustom(
    type: 'success' | 'error' | 'warning' | 'info',
    title: string,
    message: string,
    actions?: NotificationAction[],
    data?: any
  ): void {
    this.addNotification(type, title, message, actions, data);
  }

  /**
   * Get all notifications
   */
  getNotifications(): Notification[] {
    return this.notificationsSubject.value;
  }

  /**
   * Get unread notifications
   */
  getUnreadNotifications(): Notification[] {
    return this.notificationsSubject.value.filter(n => !n.read);
  }

  /**
   * Mark notification as read
   */
  markAsRead(notificationId: string): void {
    const notifications = this.notificationsSubject.value;
    const notification = notifications.find(n => n.id === notificationId);
    
    if (notification && !notification.read) {
      notification.read = true;
      this.notificationsSubject.next([...notifications]);
      this.updateUnreadCount();
    }
  }

  /**
   * Mark all notifications as read
   */
  markAllAsRead(): void {
    const notifications = this.notificationsSubject.value.map(n => ({
      ...n,
      read: true
    }));
    
    this.notificationsSubject.next(notifications);
    this.unreadCountSubject.next(0);
  }

  /**
   * Remove notification
   */
  removeNotification(notificationId: string): void {
    const notifications = this.notificationsSubject.value.filter(n => n.id !== notificationId);
    this.notificationsSubject.next(notifications);
    this.updateUnreadCount();
  }

  /**
   * Clear all notifications
   */
  clearAll(): void {
    this.notificationsSubject.next([]);
    this.unreadCountSubject.next(0);
  }

  /**
   * Clear old notifications (older than specified days)
   */
  clearOldNotifications(days: number = 7): void {
    const cutoffDate = new Date();
    cutoffDate.setDate(cutoffDate.getDate() - days);
    
    const notifications = this.notificationsSubject.value.filter(
      n => n.timestamp > cutoffDate
    );
    
    this.notificationsSubject.next(notifications);
    this.updateUnreadCount();
  }

  private showSnackBar(message: string, type: string, duration: number): void {
    const config: MatSnackBarConfig = {
      duration,
      horizontalPosition: 'right',
      verticalPosition: 'top',
      panelClass: [`snackbar-${type}`]
    };

    this.snackBar.open(message, 'Close', config);
  }

  private addNotification(
    type: 'success' | 'error' | 'warning' | 'info',
    title: string,
    message: string,
    actions?: NotificationAction[],
    data?: any
  ): void {
    const notification: Notification = {
      id: this.generateId(),
      type,
      title,
      message,
      timestamp: new Date(),
      read: false,
      actions,
      data
    };

    const notifications = [notification, ...this.notificationsSubject.value];
    
    // Keep only the last 50 notifications
    if (notifications.length > 50) {
      notifications.splice(50);
    }

    this.notificationsSubject.next(notifications);
    this.updateUnreadCount();
  }

  private updateUnreadCount(): void {
    const unreadCount = this.notificationsSubject.value.filter(n => !n.read).length;
    this.unreadCountSubject.next(unreadCount);
  }

  private generateId(): string {
    return Math.random().toString(36).substr(2, 9) + Date.now().toString(36);
  }
}