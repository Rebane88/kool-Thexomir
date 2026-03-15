export interface User {
  id: string;
  email: string;
  roles: string[];
}

export interface LoginResponse {
  userId: string;
  email: string;
  roles: string[];
  accessToken: string;
}

export interface RegisterResponse {
  userId: string;
  email: string;
  roles: string[];
}

export interface ProblemDetails {
  status: number;
  title: string;
  detail: string;
}
