import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Don't attach token for login endpoint
    // Adjust this check if your login URL changes
    if (req.url.endsWith('/login') || req.url.includes('/api/v1/Auth/login')) {
      return next.handle(req);
    }

    const token = localStorage.getItem('access_token'); // Or get from a centralized auth-storage service
    if (token) {
      const cloned = req.clone({
        setHeaders: {
          Authorization: `Bearer ${token}`
        }
      });
      return next.handle(cloned);
    }
    return next.handle(req);
  }
} 