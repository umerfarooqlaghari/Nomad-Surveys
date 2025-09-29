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
    if (!user || user === 'undefined' || user === 'null') return null;
    try {
      return JSON.parse(user);
    } catch (error) {
      console.warn('Failed to parse user from localStorage:', error);
      return null;
    }
  }

  static setTenant(tenant: any): void {
    localStorage.setItem('tenant', JSON.stringify(tenant));
  }

  static getTenant(): any | null {
    if (typeof window === 'undefined') return null;
    const tenant = localStorage.getItem('tenant');
    if (!tenant || tenant === 'undefined' || tenant === 'null') return null;
    try {
      return JSON.parse(tenant);
    } catch (error) {
      console.warn('Failed to parse tenant from localStorage:', error);
      return null;
    }
  }

  static clearAuth(): void {
    if (typeof window === 'undefined') return;
    localStorage.removeItem('authToken');
    localStorage.removeItem('user');
    localStorage.removeItem('tenant');
    localStorage.removeItem('refreshToken');
    localStorage.removeItem('tokenExpiry');
  }

  static validateAndCleanAuth(): void {
    if (typeof window === 'undefined') return;

    // Check and clean corrupted localStorage values
    const items = ['user', 'tenant', 'tokenExpiry'];
    items.forEach(item => {
      const value = localStorage.getItem(item);
      if (value === 'undefined' || value === 'null') {
        localStorage.removeItem(item);
      }
    });
  }

  static isTokenExpired(): boolean {
    if (typeof window === 'undefined') return true;
    const expiry = localStorage.getItem('tokenExpiry');
    if (!expiry || expiry === 'undefined' || expiry === 'null') return true;
    try {
      return new Date().getTime() > parseInt(expiry);
    } catch (error) {
      console.warn('Failed to parse token expiry:', error);
      return true;
    }
  }

  static setTokenExpiry(expiresAt: string): void {
    const expiryTime = new Date(expiresAt).getTime();
    localStorage.setItem('tokenExpiry', expiryTime.toString());
  }

  static getUserRole(): string | null {
    const user = this.getUser();
    // Handle both camelCase and PascalCase
    const roles = user?.roles || user?.Roles || [];
    return roles[0] || null;
  }

  static getDashboardRoute(user?: any): string {
    // Accept user as parameter to avoid localStorage race conditions
    const role = user?.roles?.[0] || this.getUserRole();
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
