import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { NotificationService } from '../services/notification.service';
import { AuthService } from '../services/auth.service';

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const notificationService = inject(NotificationService);
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      let errorMessage = 'An unexpected error occurred';

      switch (error.status) {
        case 400:
          errorMessage = error.error?.message || 'Bad request';
          break;
        case 401:
          errorMessage = 'Unauthorized access';
          authService.logout();
          router.navigate(['/login']);
          break;
        case 403:
          errorMessage = 'Access forbidden';
          break;
        case 404:
          errorMessage = 'Resource not found';
          break;
        case 500:
          errorMessage = 'Internal server error';
          break;
        case 503:
          errorMessage = 'Service temporarily unavailable';
          break;
        default:
          if (error.error?.message) {
            errorMessage = error.error.message;
          }
      }

      // Show error notification for non-401 errors (401 is handled by redirect)
      if (error.status !== 401) {
        notificationService.showError(errorMessage, 'Error');
      }

      return throwError(() => error);
    })
  );
};