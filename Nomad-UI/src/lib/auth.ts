/* eslint-disable @typescript-eslint/no-explicit-any */
import { LoginRequest, SuperAdminLoginRequest, LoginResponse } from '@/types/auth';

const API_BASE_URL = process.env.NEXT_PUBLIC_API_URL || 'http://localhost:5231/api';

export class AuthService {
  static async login(credentials: LoginRequest): Promise<LoginResponse> {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(credentials),
    });

    if (!response.ok) {
      const errorText = await response.text();
      try {
        const error = JSON.parse(errorText);
        throw new Error(error.message || error.title || 'Login failed');
      } catch {
        throw new Error(`Login failed: ${response.status} ${response.statusText}`);
      }
    }

    return response.json();
  }

  static async superAdminLogin(credentials: SuperAdminLoginRequest): Promise<LoginResponse> {
    const response = await fetch(`${API_BASE_URL}/auth/superadmin/login`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(credentials),
    });

    if (!response.ok) {
      const error = await response.json();
      throw new Error(error.message || 'SuperAdmin login failed');
    }

    return response.json();
  }

  static setToken(token: string): void {
    localStorage.setItem('authToken', token);
  }

  static getToken(): string | null {
    if (typeof window === 'undefined') return null;
    return localStorage.getItem('authToken');
  }

  static setUser(user: any): void {
    localStorage.setItem('user', JSON.stringify(user));
  }

  static getUser(): any | null {
    if (typeof window === 'undefined') return null;
    const user = localStorage.getItem('user');
    return user ? JSON.parse(user) : null;
  }

  static setTenant(tenant: any): void {
    localStorage.setItem('tenant', JSON.stringify(tenant));
  }

  static getTenant(): any | null {
    if (typeof window === 'undefined') return null;
    const tenant = localStorage.getItem('tenant');
    return tenant ? JSON.parse(tenant) : null;
  }

  static clearAuth(): void {
    localStorage.removeItem('authToken');
    localStorage.removeItem('user');
    localStorage.removeItem('tenant');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('tokenExpiry');
  }

  static isTokenExpired(): boolean {
    const expiry = localStorage.getItem('tokenExpiry');
    if (!expiry) return true;
    return new Date().getTime() > parseInt(expiry);
  }

  static setTokenExpiry(expiresAt: string): void {
    const expiryTime = new Date(expiresAt).getTime();
    localStorage.setItem('tokenExpiry', expiryTime.toString());
  }

  static getUserRole(): string | null {
    const user = this.getUser();
    return user?.roles?.[0] || null;
  }

  static getDashboardRoute(): string {
    const role = this.getUserRole();
    switch (role) {
      case 'SuperAdmin':
        return '/superadmin/dashboard';
      case 'TenantAdmin':
        return '/admin/dashboard';
      case 'Participant':
        return '/participant/dashboard';
      default:
        return '/login';
    }
  }
}
