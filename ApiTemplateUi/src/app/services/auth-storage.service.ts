import { Injectable } from '@angular/core';
import { AuthUser } from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthStorageService {
  private readonly TOKEN_KEY = 'auth_token';
  private readonly USER_KEY = 'auth_user';
  private readonly LOGIN_DATE_KEY = 'login_date';
  private readonly ROLES_KEY = 'user_roles';

  constructor() { }

  saveAuthData(authData: AuthUser): void {
    localStorage.setItem(this.TOKEN_KEY, authData.token);
    localStorage.setItem(this.LOGIN_DATE_KEY, authData.loginDate);
    localStorage.setItem(this.ROLES_KEY, JSON.stringify(authData.roles));

    if (authData.username) {
      localStorage.setItem(this.USER_KEY, authData.username);
    }
  }

  getToken(): string | null {
    return localStorage.getItem(this.TOKEN_KEY);
  }

  getUser(): string | null {
    return localStorage.getItem(this.USER_KEY);
  }

  getLoginDate(): string | null {
    return localStorage.getItem(this.LOGIN_DATE_KEY);
  }

  getRoles(): string[] {
    const rolesJson = localStorage.getItem(this.ROLES_KEY);
    return rolesJson ? JSON.parse(rolesJson) : [];
  }

  isAuthenticated(): boolean {
    return this.getToken() !== null;
  }

  clearAuthData(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    localStorage.removeItem(this.LOGIN_DATE_KEY);
    localStorage.removeItem(this.ROLES_KEY);
  }

  hasRole(role: string): boolean {
    const roles = this.getRoles();
    return roles.includes(role);
  }
}
