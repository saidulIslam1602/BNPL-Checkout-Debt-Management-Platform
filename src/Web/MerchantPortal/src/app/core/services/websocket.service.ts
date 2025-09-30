import { Injectable } from '@angular/core';
import { Observable, Subject, BehaviorSubject, fromEvent, NEVER } from 'rxjs';
import { webSocket, WebSocketSubject } from 'rxjs/webSocket';
import { retry, catchError, tap, filter, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';

export interface WebSocketMessage {
  type: string;
  data: any;
  timestamp: Date;
  id?: string;
}

export interface WebSocketConfig {
  url: string;
  reconnectInterval: number;
  maxReconnectAttempts: number;
  heartbeatInterval: number;
}

@Injectable({
  providedIn: 'root'
})
export class WebSocketService {
  private socket$!: WebSocketSubject<any>;
  private messagesSubject$ = new Subject<WebSocketMessage>();
  private connectionStatusSubject$ = new BehaviorSubject<'connected' | 'disconnected' | 'connecting'>('disconnected');
  
  public messages$ = this.messagesSubject$.asObservable();
  public connectionStatus$ = this.connectionStatusSubject$.asObservable();

  private config: WebSocketConfig = {
    url: environment.wsUrl || 'ws://localhost:5001/ws',
    reconnectInterval: 5000,
    maxReconnectAttempts: 10,
    heartbeatInterval: 30000
  };

  private reconnectAttempts = 0;
  private heartbeatTimer?: any;
  private isConnecting = false;

  /**
   * Connect to WebSocket with specified channel
   */
  connect(channel: string = 'default'): Observable<'connected' | 'disconnected' | 'connecting'> {
    if (this.isConnecting || this.connectionStatusSubject$.value === 'connected') {
      return this.connectionStatus$;
    }

    this.isConnecting = true;
    this.connectionStatusSubject$.next('connecting');

    const wsUrl = `${this.config.url}?channel=${channel}`;
    
    this.socket$ = webSocket({
      url: wsUrl,
      openObserver: {
        next: () => {
          console.log('WebSocket connected');
          this.connectionStatusSubject$.next('connected');
          this.reconnectAttempts = 0;
          this.isConnecting = false;
          this.startHeartbeat();
        }
      },
      closeObserver: {
        next: () => {
          console.log('WebSocket disconnected');
          this.connectionStatusSubject$.next('disconnected');
          this.isConnecting = false;
          this.stopHeartbeat();
          this.handleReconnection();
        }
      }
    });

    // Subscribe to messages
    this.socket$.pipe(
      retry({
        count: this.config.maxReconnectAttempts,
        delay: this.config.reconnectInterval
      }),
      catchError(error => {
        console.error('WebSocket error:', error);
        this.connectionStatusSubject$.next('disconnected');
        this.isConnecting = false;
        return NEVER;
      })
    ).subscribe({
      next: (message) => this.handleMessage(message),
      error: (error) => {
        console.error('WebSocket subscription error:', error);
        this.connectionStatusSubject$.next('disconnected');
        this.isConnecting = false;
      }
    });

    return this.connectionStatus$;
  }

  /**
   * Disconnect from WebSocket
   */
  disconnect(): void {
    this.stopHeartbeat();
    if (this.socket$) {
      this.socket$.complete();
    }
    this.connectionStatusSubject$.next('disconnected');
    this.reconnectAttempts = 0;
    this.isConnecting = false;
  }

  /**
   * Send message through WebSocket
   */
  sendMessage(type: string, data: any): void {
    if (this.connectionStatusSubject$.value === 'connected' && this.socket$) {
      const message: WebSocketMessage = {
        type,
        data,
        timestamp: new Date(),
        id: this.generateMessageId()
      };
      
      this.socket$.next(message);
    } else {
      console.warn('Cannot send message: WebSocket not connected');
    }
  }

  /**
   * Listen for specific message types
   */
  onMessage(messageType: string): Observable<any> {
    return this.messages$.pipe(
      filter(message => message.type === messageType),
      map(message => message.data)
    );
  }

  /**
   * Subscribe to transaction updates
   */
  subscribeToTransactions(): Observable<any> {
    return this.onMessage('transaction-update');
  }

  /**
   * Subscribe to new transactions
   */
  subscribeToNewTransactions(): Observable<any> {
    return this.onMessage('new-transaction');
  }

  /**
   * Subscribe to risk alerts
   */
  subscribeToRiskAlerts(): Observable<any> {
    return this.onMessage('risk-alert');
  }

  /**
   * Subscribe to fraud alerts
   */
  subscribeToFraudAlerts(): Observable<any> {
    return this.onMessage('fraud-alert');
  }

  /**
   * Subscribe to settlement updates
   */
  subscribeToSettlements(): Observable<any> {
    return this.onMessage('settlement-update');
  }

  /**
   * Subscribe to system notifications
   */
  subscribeToSystemNotifications(): Observable<any> {
    return this.onMessage('system-notification');
  }

  /**
   * Join a specific room/channel
   */
  joinRoom(roomId: string): void {
    this.sendMessage('join-room', { roomId });
  }

  /**
   * Leave a specific room/channel
   */
  leaveRoom(roomId: string): void {
    this.sendMessage('leave-room', { roomId });
  }

  /**
   * Get connection status
   */
  isConnected(): boolean {
    return this.connectionStatusSubject$.value === 'connected';
  }

  private handleMessage(message: any): void {
    try {
      const parsedMessage: WebSocketMessage = {
        type: message.type || 'unknown',
        data: message.data || message,
        timestamp: message.timestamp ? new Date(message.timestamp) : new Date(),
        id: message.id
      };

      this.messagesSubject$.next(parsedMessage);

      // Handle special message types
      switch (parsedMessage.type) {
        case 'heartbeat':
          this.handleHeartbeat();
          break;
        case 'error':
          console.error('WebSocket server error:', parsedMessage.data);
          break;
        case 'reconnect':
          console.log('Server requested reconnection');
          this.handleReconnection();
          break;
        default:
          // Regular message, already emitted
          break;
      }
    } catch (error) {
      console.error('Error parsing WebSocket message:', error);
    }
  }

  private handleHeartbeat(): void {
    // Respond to server heartbeat
    this.sendMessage('heartbeat-response', { timestamp: new Date() });
  }

  private startHeartbeat(): void {
    this.stopHeartbeat();
    this.heartbeatTimer = setInterval(() => {
      if (this.isConnected()) {
        this.sendMessage('heartbeat', { timestamp: new Date() });
      }
    }, this.config.heartbeatInterval);
  }

  private stopHeartbeat(): void {
    if (this.heartbeatTimer) {
      clearInterval(this.heartbeatTimer);
      this.heartbeatTimer = null;
    }
  }

  private handleReconnection(): void {
    if (this.reconnectAttempts < this.config.maxReconnectAttempts) {
      this.reconnectAttempts++;
      console.log(`Attempting to reconnect (${this.reconnectAttempts}/${this.config.maxReconnectAttempts})...`);
      
      setTimeout(() => {
        if (this.connectionStatusSubject$.value === 'disconnected') {
          this.connect();
        }
      }, this.config.reconnectInterval);
    } else {
      console.error('Max reconnection attempts reached');
      this.connectionStatusSubject$.next('disconnected');
    }
  }

  private generateMessageId(): string {
    return Math.random().toString(36).substr(2, 9) + Date.now().toString(36);
  }

  /**
   * Update WebSocket configuration
   */
  updateConfig(newConfig: Partial<WebSocketConfig>): void {
    this.config = { ...this.config, ...newConfig };
  }

  /**
   * Get current configuration
   */
  getConfig(): WebSocketConfig {
    return { ...this.config };
  }

  /**
   * Get connection statistics
   */
  getConnectionStats(): {
    status: string;
    reconnectAttempts: number;
    maxReconnectAttempts: number;
    lastConnected?: Date;
  } {
    return {
      status: this.connectionStatusSubject$.value,
      reconnectAttempts: this.reconnectAttempts,
      maxReconnectAttempts: this.config.maxReconnectAttempts
    };
  }
}