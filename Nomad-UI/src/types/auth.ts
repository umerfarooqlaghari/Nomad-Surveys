export interface User {
  id: string;
  userName: string;
  email: string;
  firstName: string;
  lastName: string;
  fullName: string;
  isActive: boolean;
  emailConfirmed: boolean;
  phoneNumber?: string;
  createdAt: string;
  updatedAt?: string;
  lastLoginAt?: string;
  tenantId?: string;
  roles: string[];
  permissions: string[];
  tenant?: Tenant;
}

export interface Tenant {
  id: string;
  name: string;
  slug: string;
  description?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  tenantSlug?: string;
  rememberMe?: boolean;
}

export interface SuperAdminLoginRequest {
  email: string;
  password: string;
  rememberMe?: boolean;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: User;
  tenant?: Tenant;
}

export interface AuthContextType {
  user: User | null;
  tenant: Tenant | null;
  token: string | null;
  isAuthenticated: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  superAdminLogin: (credentials: SuperAdminLoginRequest) => Promise<void>;
  logout: () => void;
  isLoading: boolean;
}

export type UserRole = 'SuperAdmin' | 'TenantAdmin' | 'Participant';
