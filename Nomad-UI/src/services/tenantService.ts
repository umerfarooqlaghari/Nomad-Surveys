/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, ApiResponse, handleApiResponse } from './api';

// Types for Tenant API
export interface CreateTenantData {
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
    PhoneNumber: string;
    Password: string;
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
  // TenantAdmin is nullable since admin details are not editable in the UI during updates
  TenantAdmin: {
    FirstName: string;
    LastName: string;
    Email: string;
    // Password and PhoneNumber are NOT included in updates
  } | null;
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
    if (!data.Name.trim()) {
      errors.push('Organization name is required');
    }
    if (!data.Slug.trim()) {
      errors.push('Tenant slug is required');
    }

    // Validate company info
    if (!data.Company.Name.trim()) {
      errors.push('Company name is required');
    }
    if (data.Company.NumberOfEmployees < 1) {
      errors.push('Number of employees must be at least 1');
    }
    if (!data.Company.Location.trim()) {
      errors.push('Company location is required');
    }
    if (!data.Company.Industry.trim()) {
      errors.push('Company industry is required');
    }
    if (!data.Company.ContactPersonName.trim()) {
      errors.push('Contact person name is required');
    }
    if (!data.Company.ContactPersonEmail.trim()) {
      errors.push('Contact person email is required');
    }
    if (!data.Company.ContactPersonRole.trim()) {
      errors.push('Contact person role is required');
    }
    if (!data.Company.ContactPersonPhone.trim()) {
      errors.push('Contact person phone number is required');
    }

    // Validate email format
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (data.Company.ContactPersonEmail && !emailRegex.test(data.Company.ContactPersonEmail)) {
      errors.push('Contact person email format is invalid');
    }

    // Validate tenant admin
    if (!data.TenantAdmin.FirstName.trim()) {
      errors.push('Tenant admin first name is required');
    }
    if (!data.TenantAdmin.LastName.trim()) {
      errors.push('Tenant admin last name is required');
    }
    if (!data.TenantAdmin.Email.trim()) {
      errors.push('Tenant admin email is required');
    }
    if (data.TenantAdmin.Email && !emailRegex.test(data.TenantAdmin.Email)) {
      errors.push('Tenant admin email format is invalid');
    }
    if (!data.TenantAdmin.PhoneNumber.trim()) {
      errors.push('Tenant admin phone number is required');
    }
    if (!data.TenantAdmin.Password || data.TenantAdmin.Password.length < 6) {
      errors.push('Tenant admin password must be at least 6 characters');
    }

    // Validate logo URL if provided
    if (data.Company.LogoUrl) {
      try {
        new URL(data.Company.LogoUrl);
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
      Name: '',
      Slug: '',
      Description: '',
      Company: {
        Name: '',
        NumberOfEmployees: 1,
        Location: '',
        Industry: '',
        ContactPersonName: '',
        ContactPersonEmail: '',
        ContactPersonRole: '',
        ContactPersonPhone: '',
        LogoUrl: '',
      },
      TenantAdmin: {
        FirstName: '',
        LastName: '',
        Email: '',
        PhoneNumber: '',
        Password: '',
      },
    };
  }
}

// Create and export a singleton instance
export const tenantService = new TenantService();

// Export the class for testing or custom instances
export { TenantService };
