import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ApiResponse, LoginRequest, LoginResponse } from '../models/auth.models';
import { ApiGetterService } from './shared/api-getter.service';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = 'http://localhost:5024/api/v1/Auth';

  constructor(private apiHandler:ApiGetterService) { }

  login(credentials: LoginRequest): Observable<ApiResponse<LoginResponse>> {
    return this.apiHandler.post<ApiResponse<LoginResponse>>(
      `${this.apiUrl}/login`,
      credentials
    );
  }

  logout(): Observable<string> {
    return this.apiHandler.post<string>(`${this.apiUrl}/logout`, {});
  }
}
