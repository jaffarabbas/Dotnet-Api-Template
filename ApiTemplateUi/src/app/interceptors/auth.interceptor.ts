import { Injectable } from '@angular/core';
import { HttpInterceptor, HttpRequest, HttpHandler, HttpEvent } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    // Skip attaching token for login endpoint or when caller explicitly set X-Skip-Auth
    // Adjust this check if your login URL changes
    if (req.url.endsWith('/login') || req.url.includes('/api/v1/Auth/login')) {
      // If request included the skip header, remove it before forwarding
      if (req.headers.has('X-Skip-Auth')) {
        const cleaned = req.clone({ headers: req.headers.delete('X-Skip-Auth') });
        return next.handle(cleaned);
      }
      return next.handle(req);
    }

    // Honor explicit skip header too
    if (req.headers.has('X-Skip-Auth')) {
      const cleaned = req.clone({ headers: req.headers.delete('X-Skip-Auth') });
      return next.handle(cleaned);
    }

    // Read token from localStorage `auth_token`
    const token = localStorage.getItem('auth_token'); // updated token key
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