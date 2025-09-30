import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { BehaviorSubject, Observable, of, throwError } from 'rxjs';
import { catchError, tap, map } from 'rxjs/operators';
import { environment } from '../../../environments/environment';
import { User, UserPreferences, LoginRequest, LoginResponse, AuthState } from '../models/user.model';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private http = inject(HttpClient);
  private router = inject(Router);
  
  private readonly baseUrl = `${environment.apiUrl}/auth`;
  private readonly TOKEN_KEY = 'yourcompany_token';
  private readonly REFRESH_TOKEN_KEY = 'yourcompany_refresh_token';
  private readonly USER_KEY = 'yourcompany_user';

  private authStateSubject = new BehaviorSubject<AuthState>({
    isAuthenticated: false,
    user: null,
    token: null,
    refreshToken: null
  });

  public authState$ = this.authStateSubject.asObservable();
  public isAuthenticated$ = this.authState$.pipe(map(state => state.isAuthenticated));
  public currentUser$ = this.authState$.pipe(map(state => state.user));

  constructor() {
    this.initializeAuth();
  }

  /**
   * Initialize authentication state from localStorage
   */
  initializeAuth(): void {
    const token = localStorage.getItem(this.TOKEN_KEY);
    const refreshToken = localStorage.getItem(this.REFRESH_TOKEN_KEY);
    const userJson = localStorage.getItem(this.USER_KEY);

    if (token && userJson) {
      try {
        const user = JSON.parse(userJson);
        
        // Check if token is expired
        if (this.isTokenExpired(token)) {
          if (refreshToken) {
            this.refreshAuthToken(refreshToken).subscribe({
              next: () => {
                // Token refreshed successfully
              },
              error: () => {
                this.logout();
              }
            });
          } else {
            this.logout();
          }
        } else {
          this.updateAuthState({
            isAuthenticated: true,
            user,
            token,
            refreshToken
          });
        }
      } catch (error) {
        console.error('Error parsing stored user data:', error);
        this.logout();
      }
    }
  }

  /**
   * Login with email and password
   */
  login(credentials: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.baseUrl}/login`, credentials).pipe(
      tap(response => {
        this.handleAuthSuccess(response);
      }),
      catchError(error => {
        console.error('Login error:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Logout user
   */
  logout(): void {
    // Call logout endpoint to invalidate token on server
    const token = this.authStateSubject.value.token;
    if (token) {
      this.http.post(`${this.baseUrl}/logout`, {}).pipe(
        catchError(error => {
          console.error('Logout error:', error);
          return of(null);
        })
      ).subscribe();
    }

    // Clear local storage
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.REFRESH_TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);

    // Update auth state
    this.updateAuthState({
      isAuthenticated: false,
      user: null,
      token: null,
      refreshToken: null
    });

    // Navigate to login
    this.router.navigate(['/login']);
  }

  /**
   * Refresh authentication token
   */
  refreshAuthToken(refreshToken?: string): Observable<LoginResponse> {
    const token = refreshToken || this.authStateSubject.value.refreshToken;
    
    if (!token) {
      return throwError(() => new Error('No refresh token available'));
    }

    return this.http.post<LoginResponse>(`${this.baseUrl}/refresh`, { 
      refreshToken: token 
    }).pipe(
      tap(response => {
        this.handleAuthSuccess(response);
      }),
      catchError(error => {
        console.error('Token refresh error:', error);
        this.logout();
        return throwError(() => error);
      })
    );
  }

  /**
   * Get current user
   */
  getCurrentUser(): User | null {
    return this.authStateSubject.value.user;
  }

  /**
   * Get current auth token
   */
  getToken(): string | null {
    return this.authStateSubject.value.token;
  }

  /**
   * Check if user is authenticated
   */
  isAuthenticated(): boolean {
    return this.authStateSubject.value.isAuthenticated;
  }

  /**
   * Check if user has specific permission
   */
  hasPermission(permission: string): boolean {
    const user = this.getCurrentUser();
    return user?.permissions?.includes(permission) || false;
  }

  /**
   * Check if user has specific role
   */
  hasRole(role: string): boolean {
    const user = this.getCurrentUser();
    return user?.role === role;
  }

  /**
   * Update user profile
   */
  updateProfile(profileData: Partial<User>): Observable<User> {
    return this.http.patch<User>(`${this.baseUrl}/profile`, profileData).pipe(
      tap(updatedUser => {
        const currentState = this.authStateSubject.value;
        this.updateAuthState({
          ...currentState,
          user: updatedUser
        });
        
        // Update localStorage
        localStorage.setItem(this.USER_KEY, JSON.stringify(updatedUser));
      }),
      catchError(error => {
        console.error('Profile update error:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Update user preferences
   */
  updatePreferences(preferences: Partial<UserPreferences>): Observable<UserPreferences> {
    return this.http.patch<UserPreferences>(`${this.baseUrl}/preferences`, preferences).pipe(
      tap(updatedPreferences => {
        const currentState = this.authStateSubject.value;
        if (currentState.user) {
          const updatedUser = {
            ...currentState.user,
            preferences: updatedPreferences
          };
          
          this.updateAuthState({
            ...currentState,
            user: updatedUser
          });
          
          // Update localStorage
          localStorage.setItem(this.USER_KEY, JSON.stringify(updatedUser));
        }
      }),
      catchError(error => {
        console.error('Preferences update error:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Change password
   */
  changePassword(currentPassword: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/change-password`, {
      currentPassword,
      newPassword
    }).pipe(
      catchError(error => {
        console.error('Password change error:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Request password reset
   */
  requestPasswordReset(email: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/forgot-password`, { email }).pipe(
      catchError(error => {
        console.error('Password reset request error:', error);
        return throwError(() => error);
      })
    );
  }

  /**
   * Reset password with token
   */
  resetPassword(token: string, newPassword: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/reset-password`, {
      token,
      newPassword
    }).pipe(
      catchError(error => {
        console.error('Password reset error:', error);
        return throwError(() => error);
      })
    );
  }

  private handleAuthSuccess(response: LoginResponse): void {
    // Store tokens and user data
    localStorage.setItem(this.TOKEN_KEY, response.token);
    localStorage.setItem(this.REFRESH_TOKEN_KEY, response.refreshToken);
    localStorage.setItem(this.USER_KEY, JSON.stringify(response.user));

    // Update auth state
    this.updateAuthState({
      isAuthenticated: true,
      user: response.user,
      token: response.token,
      refreshToken: response.refreshToken
    });
  }

  private updateAuthState(newState: AuthState): void {
    this.authStateSubject.next(newState);
  }

  private isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const expirationTime = payload.exp * 1000; // Convert to milliseconds
      return Date.now() >= expirationTime;
    } catch (error) {
      console.error('Error parsing token:', error);
      return true; // Assume expired if we can't parse
    }
  }

  /**
   * Get user's preferred language
   */
  getPreferredLanguage(): string {
    const user = this.getCurrentUser();
    return user?.preferences?.language || 'en';
  }

  /**
   * Get user's timezone
   */
  getTimezone(): string {
    const user = this.getCurrentUser();
    return user?.preferences?.timezone || 'Europe/Oslo';
  }

  /**
   * Check if user has notification preferences enabled
   */
  hasNotificationEnabled(type: 'email' | 'sms' | 'push'): boolean {
    const user = this.getCurrentUser();
    return user?.preferences?.notifications?.[type] || false;
  }
}