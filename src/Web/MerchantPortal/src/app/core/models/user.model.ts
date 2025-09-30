export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  merchantId: string;
  merchantName: string;
  role: 'admin' | 'manager' | 'operator' | 'viewer';
  permissions: string[];
  lastLoginAt?: Date;
  createdAt: Date;
  isActive: boolean;
  preferences?: UserPreferences;
}

export interface UserPreferences {
  language: 'en' | 'no';
  timezone: string;
  currency: string;
  notifications: {
    email: boolean;
    sms: boolean;
    push: boolean;
  };
  dashboard: {
    defaultView: string;
    refreshInterval: number;
  };
}

export interface LoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponse {
  user: User;
  token: string;
  refreshToken: string;
  expiresIn: number;
}

export interface AuthState {
  isAuthenticated: boolean;
  user: User | null;
  token: string | null;
  refreshToken: string | null;
}