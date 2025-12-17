/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, ApiResponse, handleApiResponse } from './api';

// Types for Tenant API
export interface CreateTenantData {
  name: string;
  slug: string;
  description: string;
  company: {
    name: string;
    numberOfEmployees: number;
    location: string;
    industry: string;
    contactPersonName: string;
    contactPersonEmail: string;
    contactPersonRole: string;
    contactPersonPhone: string;
    logoUrl: string;
  };
  tenantAdmin: {
    firstName: string;
    lastName: string;
    email: string;
    phoneNumber: string;
    password: string;
  };
}

export interface TenantListItem {
  Id: string;
  Name: string;
  Slug: string;
  IsActive: boolean;
  CreatedAt: string;
  UserCount: number;
  CompanyName: string;
  LogoUrl?: string;
}

export interface TenantResponse {
  Id: string;
  Name: string;
  Slug: string;
  Description: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  Company?: {
    Id: string;
    Name: string;
    NumberOfEmployees: number;
    Location: string;
    Industry: string;
    ContactPersonName: string;
    ContactPersonEmail: string;
    ContactPersonRole: string;
    ContactPersonPhone: string;
    LogoUrl?: string;
    ContactPerson?: {
      Id: string;
      UserName: string;
      Email: string;
      FirstName: string;
      LastName: string;
      FullName: string;
      IsActive: boolean;
      EmailConfirmed: boolean;
      PhoneNumber?: string;
      CreatedAt: string;
      UpdatedAt?: string;
      LastLoginAt?: string;
      TenantId?: string;
    };
  };
  TenantAdmin?: {
    Id: string;
    UserName: string;
    Email: string;
    FirstName: string;
    LastName: string;
    FullName: string;
    IsActive: boolean;
    EmailConfirmed: boolean;
    PhoneNumber?: string;
    CreatedAt: string;
    UpdatedAt?: string;
    LastLoginAt?: string;
    TenantId?: string;
    Roles: string[];
    Permissions: string[];
  };
}

export interface UpdateTenantData {
  Name: string;
  Slug: string;
  Description: string;
  Company: {
    Name: string;
    NumberOfEmployees: number;
    Location: string;
    Industry: string;
    ContactPersonName: string;
    ContactPersonEmail: string;
    ContactPersonRole: string;
    ContactPersonPhone: string;
    LogoUrl: string;
  };
  TenantAdmin: {
    FirstName: string;
    LastName: string;
    Email: string;
    // Password and PhoneNumber are NOT included in updates
  };
}

class TenantService {
  private readonly endpoint = '/tenant';

  /**
   * Get all tenants (SuperAdmin only)
   */
  async getTenants(token: string): Promise<{ data: TenantListItem[] | null; error: string | null }> {
    const response = await apiClient.get<TenantListItem[]>(this.endpoint, token);
    return handleApiResponse(response);
  }

  /**
   * Get tenant by ID (SuperAdmin only)
   */
  async getTenantById(id: string, token: string): Promise<{ data: TenantResponse | null; error: string | null }> {
    const response = await apiClient.get<TenantResponse>(`${this.endpoint}/${id}`, token);
    return handleApiResponse(response);
  }

  /**
   * Get tenant by slug (SuperAdmin only)
   */
  async getTenantBySlug(slug: string, token: string): Promise<{ data: TenantResponse | null; error: string | null }> {
    const response = await apiClient.get<TenantResponse>(`${this.endpoint}/by-slug/${slug}`, token);
    return handleApiResponse(response);
  }

  /**
   * Create a new tenant (SuperAdmin only)
   */
  async createTenant(data: CreateTenantData, token: string): Promise<{ data: TenantResponse | null; error: string | null }> {
    const response = await apiClient.post<TenantResponse>(this.endpoint, data, token);
    return handleApiResponse(response);
  }

  /**
   * Update tenant (SuperAdmin only)
   */
  async updateTenant(id: string, data: UpdateTenantData, token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.put(`${this.endpoint}/${id}`, data, token);
    return handleApiResponse(response);
  }

  /**
   * Delete tenant (SuperAdmin only)
   */
  async deleteTenant(id: string, token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.delete(`${this.endpoint}/${id}`, token);
    return handleApiResponse(response);
  }

  /**
   * Activate tenant (SuperAdmin only)
   */
  async activateTenant(id: string, token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.post(`${this.endpoint}/${id}/activate`, undefined, token);
    return handleApiResponse(response);
  }

  /**
   * Deactivate tenant (SuperAdmin only)
   */
  async deactivateTenant(id: string, token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.post(`${this.endpoint}/${id}/deactivate`, undefined, token);
    return handleApiResponse(response);
  }

  /**
   * Generate slug from name
   */
  generateSlug(name: string): string {
    return name
      .toLowerCase()
      .replace(/[^a-z0-9\s-]/g, '')
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .trim();
  }

  /**
   * Validate tenant data before submission
   */
  validateTenantData(data: CreateTenantData): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];

    // Validate tenant info
    if (!data.name.trim()) {
      errors.push('Organization name is required');
    }
    if (!data.slug.trim()) {
      errors.push('Tenant slug is required');
    }

    // Validate company info
    if (!data.company.name.trim()) {
      errors.push('Company name is required');
    }
    if (data.company.numberOfEmployees < 1) {
      errors.push('Number of employees must be at least 1');
    }
    if (!data.company.location.trim()) {
      errors.push('Company location is required');
    }
    if (!data.company.industry.trim()) {
      errors.push('Company industry is required');
    }
    if (!data.company.contactPersonName.trim()) {
      errors.push('Contact person name is required');
    }
    if (!data.company.contactPersonEmail.trim()) {
      errors.push('Contact person email is required');
    }
    if (!data.company.contactPersonRole.trim()) {
      errors.push('Contact person role is required');
    }

    // Validate email format
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (data.company.contactPersonEmail && !emailRegex.test(data.company.contactPersonEmail)) {
      errors.push('Contact person email format is invalid');
    }

    // Validate tenant admin
    if (!data.tenantAdmin.firstName.trim()) {
      errors.push('Tenant admin first name is required');
    }
    if (!data.tenantAdmin.lastName.trim()) {
      errors.push('Tenant admin last name is required');
    }
    if (!data.tenantAdmin.email.trim()) {
      errors.push('Tenant admin email is required');
    }
    if (data.tenantAdmin.email && !emailRegex.test(data.tenantAdmin.email)) {
      errors.push('Tenant admin email format is invalid');
    }
    if (!data.tenantAdmin.password || data.tenantAdmin.password.length < 6) {
      errors.push('Tenant admin password must be at least 6 characters');
    }

    // Validate logo URL if provided
    if (data.company.logoUrl) {
      try {
        new URL(data.company.logoUrl);
      } catch {
        errors.push('Logo URL format is invalid');
      }
    }

    return {
      isValid: errors.length === 0,
      errors,
    };
  }

  /**
   * Get default form data for creating a new tenant
   */
  getDefaultFormData(): CreateTenantData {
    return {
      name: '',
      slug: '',
      description: '',
      company: {
        name: '',
        numberOfEmployees: 1,
        location: '',
        industry: '',
        contactPersonName: '',
        contactPersonEmail: '',
        contactPersonRole: '',
        contactPersonPhone: '',
        logoUrl: '',
      },
      tenantAdmin: {
        firstName: '',
        lastName: '',
        email: '',
        phoneNumber: '',
        password: '',
      },
    };
  }
}

// Create and export a singleton instance
export const tenantService = new TenantService();

// Export the class for testing or custom instances
export { TenantService };
