export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  loginDate: string;
  roles: string[];
}

export interface ApiResponse<T> {
  StatusCode: string;
  Message: string;
  Data: T;
}

export interface AuthUser {
  token: string;
  loginDate: string;
  roles: string[];
  username?: string;
}
