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
  statusCode: string;
  message: string;
  data: T;
}

export interface AuthUser {
  token: string;
  loginDate: string;
  roles: string[];
  username?: string;
}
