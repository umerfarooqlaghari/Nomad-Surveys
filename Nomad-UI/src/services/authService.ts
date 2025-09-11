/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, handleApiResponse } from './api';
import { LoginRequest, SuperAdminLoginRequest, LoginResponse } from '@/types/auth';

class AuthService {
  private readonly authEndpoint = '/auth';

  /**
   * Regular user login
   */
  async login(credentials: LoginRequest): Promise<{ data: LoginResponse | null; error: string | null }> {
    const response = await apiClient.post<LoginResponse>(`${this.authEndpoint}/login`, credentials);
    return handleApiResponse(response);
  }

  /**
   * SuperAdmin login
   */
  async superAdminLogin(credentials: SuperAdminLoginRequest): Promise<{ data: LoginResponse | null; error: string | null }> {
    const response = await apiClient.post<LoginResponse>(`${this.authEndpoint}/superadmin/login`, credentials);
    return handleApiResponse(response);
  }

  /**
   * Tenant admin login
   */
  async tenantAdminLogin(tenantSlug: string, credentials: LoginRequest): Promise<{ data: LoginResponse | null; error: string | null }> {
    const response = await apiClient.post<LoginResponse>(`/${tenantSlug}/api/auth/login`, credentials);
    return handleApiResponse(response);
  }

  /**
   * Participant login
   */
  async participantLogin(tenantSlug: string, credentials: LoginRequest): Promise<{ data: LoginResponse | null; error: string | null }> {
    const response = await apiClient.post<LoginResponse>(`/${tenantSlug}/api/auth/participant/login`, credentials);
    return handleApiResponse(response);
  }

  /**
   * Refresh token
   */
  async refreshToken(refreshToken: string): Promise<{ data: LoginResponse | null; error: string | null }> {
    const response = await apiClient.post<LoginResponse>(`${this.authEndpoint}/refresh`, { refreshToken });
    return handleApiResponse(response);
  }

  /**
   * Logout (invalidate token)
   */
  async logout(token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.post(`${this.authEndpoint}/logout`, undefined, token);
    return handleApiResponse(response);
  }

  /**
   * Verify token validity
   */
  async verifyToken(token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.get(`${this.authEndpoint}/verify`, token);
    return handleApiResponse(response);
  }

  /**
   * Get user profile
   */
  async getProfile(token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.get(`${this.authEndpoint}/profile`, token);
    return handleApiResponse(response);
  }

  /**
   * Update user profile
   */
  async updateProfile(profileData: any, token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.put(`${this.authEndpoint}/profile`, profileData, token);
    return handleApiResponse(response);
  }

  /**
   * Change password
   */
  async changePassword(passwordData: { currentPassword: string; newPassword: string }, token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.post(`${this.authEndpoint}/change-password`, passwordData, token);
    return handleApiResponse(response);
  }

  /**
   * Request password reset
   */
  async requestPasswordReset(email: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.post(`${this.authEndpoint}/forgot-password`, { email });
    return handleApiResponse(response);
  }

  /**
   * Reset password with token
   */
  async resetPassword(resetData: { token: string; newPassword: string }): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.post(`${this.authEndpoint}/reset-password`, resetData);
    return handleApiResponse(response);
  }

  /**
   * Validate email format
   */
  validateEmail(email: string): boolean {
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }

  /**
   * Validate password strength
   */
  validatePassword(password: string): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];

    if (password.length < 6) {
      errors.push('Password must be at least 6 characters long');
    }

    if (!/[A-Z]/.test(password)) {
      errors.push('Password must contain at least one uppercase letter');
    }

    if (!/[a-z]/.test(password)) {
      errors.push('Password must contain at least one lowercase letter');
    }

    if (!/\d/.test(password)) {
      errors.push('Password must contain at least one number');
    }

    if (!/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
      errors.push('Password must contain at least one special character');
    }

    return {
      isValid: errors.length === 0,
      errors,
    };
  }

  /**
   * Check if token is expired
   */
  isTokenExpired(token: string): boolean {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Math.floor(Date.now() / 1000);
      return payload.exp < currentTime;
    } catch {
      return true; // If we can't parse the token, consider it expired
    }
  }

  /**
   * Get token expiration time
   */
  getTokenExpiration(token: string): Date | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return new Date(payload.exp * 1000);
    } catch {
      return null;
    }
  }

  /**
   * Get user info from token
   */
  getUserFromToken(token: string): any | null {
    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      return {
        id: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'],
        email: payload['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'],
        firstName: payload['FirstName'],
        lastName: payload['LastName'],
        roles: payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'],
        permissions: payload['Permission'],
      };
    } catch {
      return null;
    }
  }

  /**
   * Check if user has specific role
   */
  hasRole(token: string, role: string): boolean {
    const user = this.getUserFromToken(token);
    if (!user || !user.roles) return false;
    
    if (Array.isArray(user.roles)) {
      return user.roles.includes(role);
    }
    
    return user.roles === role;
  }

  /**
   * Check if user has specific permission
   */
  hasPermission(token: string, permission: string): boolean {
    const user = this.getUserFromToken(token);
    if (!user || !user.permissions) return false;
    
    if (Array.isArray(user.permissions)) {
      return user.permissions.includes(permission);
    }
    
    return user.permissions === permission;
  }

  /**
   * Store token in localStorage
   */
  storeToken(token: string, refreshToken?: string): void {
    localStorage.setItem('accessToken', token);
    if (refreshToken) {
      localStorage.setItem('refreshToken', refreshToken);
    }
  }

  /**
   * Get token from localStorage
   */
  getStoredToken(): string | null {
    return localStorage.getItem('accessToken');
  }

  /**
   * Get refresh token from localStorage
   */
  getStoredRefreshToken(): string | null {
    return localStorage.getItem('refreshToken');
  }

  /**
   * Remove tokens from localStorage
   */
  clearStoredTokens(): void {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
  }
}

// Create and export a singleton instance
export const authService = new AuthService();

// Export the class for testing or custom instances
export { AuthService };
