import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const token = authService.getToken();

  // Skip authentication for login and public endpoints
  const skipAuth = req.url.includes('/auth/login') || 
                   req.url.includes('/auth/register') || 
                   req.url.includes('/auth/forgot-password') ||
                   req.url.includes('/health');

  if (token && !skipAuth) {
    const authReq = req.clone({
      headers: req.headers.set('Authorization', `Bearer ${token}`)
    });
    return next(authReq);
  }

  return next(req);
};