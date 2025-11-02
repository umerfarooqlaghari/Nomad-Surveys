export interface User {
  // Support both camelCase and PascalCase for compatibility
  id?: string;
  Id?: string;
  userName?: string;
  UserName?: string;
  email?: string;
  Email?: string;
  firstName?: string;
  FirstName?: string;
  lastName?: string;
  LastName?: string;
  fullName?: string;
  FullName?: string;
  isActive?: boolean;
  IsActive?: boolean;
  emailConfirmed?: boolean;
  EmailConfirmed?: boolean;
  phoneNumber?: string;
  PhoneNumber?: string;
  createdAt?: string;
  CreatedAt?: string;
  updatedAt?: string;
  UpdatedAt?: string;
  lastLoginAt?: string;
  LastLoginAt?: string;
  tenantId?: string;
  TenantId?: string;
  roles?: string[];
  Roles?: string[];
  permissions?: string[];
  Permissions?: string[];
  tenant?: Tenant;
  Tenant?: Tenant;
}

export interface Tenant {
  // Support both camelCase and PascalCase for compatibility
  id?: string;
  Id?: string;
  name?: string;
  Name?: string;
  slug?: string;
  Slug?: string;
  description?: string;
  Description?: string;
  isActive?: boolean;
  IsActive?: boolean;
  createdAt?: string;
  CreatedAt?: string;
  updatedAt?: string;
  UpdatedAt?: string;
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
  // Support both camelCase and PascalCase for compatibility
  accessToken?: string;
  AccessToken?: string;
  refreshToken?: string;
  RefreshToken?: string;
  expiresAt?: string;
  ExpiresAt?: string;
  user?: User;
  User?: User;
  tenant?: Tenant;
  Tenant?: Tenant;
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
