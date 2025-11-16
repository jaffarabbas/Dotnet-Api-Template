import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface ApiGetterOptions {
  /** when true, the AuthInterceptor will skip attaching the Authorization header */
  skipAuth?: boolean;
  /** extra headers to include */
  headers?: { [key: string]: string };
  /** query params */
  params?: { [key: string]: any };
}

@Injectable({ providedIn: 'root' })
export class ApiGetterService {
  constructor(private http: HttpClient) {}
  private buildOptions(options?: ApiGetterOptions): { headers?: HttpHeaders; params?: HttpParams } {
    let headers = new HttpHeaders(options?.headers ?? {});
    if (options?.skipAuth) {
      headers = headers.set('X-Skip-Auth', '1');
    }

    const params = options?.params ? new HttpParams({ fromObject: options.params }) : undefined;

    return { headers, params };
  }

  get<T>(url: string, options?: ApiGetterOptions): Observable<T> {
    return this.http.get<T>(url, this.buildOptions(options));
  }

  post<T>(url: string, model: any, options?: ApiGetterOptions): Observable<T> {
    return this.http.post<T>(url, model, this.buildOptions(options));
  }

  put<T>(url: string, model: any, options?: ApiGetterOptions): Observable<T> {
    return this.http.put<T>(url, model, this.buildOptions(options));
  }

  delete<T>(url: string, options?: ApiGetterOptions): Observable<T> {
    return this.http.delete<T>(url, this.buildOptions(options));
  }
}