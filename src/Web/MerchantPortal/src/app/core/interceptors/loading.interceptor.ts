import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { finalize } from 'rxjs';
import { LoadingService } from '../services/loading.service';

export const loadingInterceptor: HttpInterceptorFn = (req, next) => {
  const loadingService = inject(LoadingService);
  
  // Skip loading indicator for certain requests
  const skipLoading = req.url.includes('/realtime') || 
                     req.url.includes('/websocket') ||
                     req.url.includes('/health') ||
                     req.headers.has('X-Skip-Loading');

  if (!skipLoading) {
    const requestId = `${req.method}-${req.url}-${Date.now()}`;
    loadingService.show(requestId);

    return next(req).pipe(
      finalize(() => loadingService.hide(requestId))
    );
  }

  return next(req);
};