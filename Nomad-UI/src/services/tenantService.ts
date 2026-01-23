/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, ApiResponse, handleApiResponse } from './api';

// Types for Tenant API
export interface CreateTenantData {
  Name: string;
  Slug: string;
  Description: string;
  Company: {
    Name: string;
    NumberOfEmployees: string | null;
    Location: string;
    Industry: string;
    ContactPersonName: string;
    ContactPersonEmail: string;
    ContactPersonRole: string | null;
    ContactPersonPhone: string | null;
    LogoUrl: string | null;
  };
  TenantAdmin: {
    FirstName: string | null;
    LastName: string | null;
    Email: string | null;
    PhoneNumber: string | null;
    Password: string | null;
  } | null;
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
    NumberOfEmployees?: string;
    Location: string;
    Industry: string;
    ContactPersonName: string;
    ContactPersonEmail: string;
    ContactPersonRole?: string;
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
    NumberOfEmployees: string | null;
    Location: string;
    Industry: string;
    ContactPersonName: string;
    ContactPersonEmail: string;
    ContactPersonRole: string | null;
    ContactPersonPhone: string | null;
    LogoUrl: string | null;
  };
  // TenantAdmin is nullable since admin details are not editable in the UI during updates
  TenantAdmin: {
    FirstName: string | null;
    LastName: string | null;
    Email: string | null;
    PhoneNumber: string | null;
    Password?: string | null;
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
    const cleanedData = this.cleanData(data);
    const response = await apiClient.post<TenantResponse>(this.endpoint, cleanedData, token);
    return handleApiResponse(response);
  }

  /**
   * Update tenant (SuperAdmin only)
   */
  async updateTenant(id: string, data: UpdateTenantData, token: string): Promise<{ data: any | null; error: string | null }> {
    const cleanedData = this.cleanData(data);
    const response = await apiClient.put(`${this.endpoint}/${id}`, cleanedData, token);
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
   * Recursively clean data by converting empty strings to null
   */
  private cleanData(obj: any): any {
    if (obj === null || obj === undefined) return obj;
    if (typeof obj !== 'object') return obj === '' ? null : obj;

    if (Array.isArray(obj)) {
      return obj.map(item => this.cleanData(item));
    }

    const cleaned: any = {};
    for (const [key, value] of Object.entries(obj)) {
      cleaned[key] = this.cleanData(value);
    }
    return cleaned;
  }

  /**
   * Validate tenant data before submission
   */
  validateTenantData(data: CreateTenantData, isEditMode: boolean = false): { isValid: boolean; errors: string[] } {
    const errors: string[] = [];

    // Validate tenant info
    if (!data.Name.trim()) {
      errors.push('Organization name is required');
    }
    if (!data.Slug.trim()) {
      errors.push('Company Code is required');
    }

    // Validate company info
    if (!data.Company.Name.trim()) {
      errors.push('Company name is required');
    }
    if (!data.Company.ContactPersonEmail.trim()) {
      errors.push('Contact person email is required');
    }

    // Validate email format
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    if (data.Company.ContactPersonEmail && !emailRegex.test(data.Company.ContactPersonEmail)) {
      errors.push('Contact person email format is invalid');
    }

    // Validate TenantAdmin with "All or Nothing" rule
    if (data.TenantAdmin) {
      const admin = data.TenantAdmin;
      const hasFirstName = !!admin.FirstName?.trim();
      const hasLastName = !!admin.LastName?.trim();
      const hasEmail = !!admin.Email?.trim();
      const hasPhoneNumber = !!admin.PhoneNumber?.trim();
      const hasPassword = !!admin.Password && admin.Password.length > 0;

      const filledFields = [hasFirstName, hasLastName, hasEmail, hasPhoneNumber];
      const filledCount = filledFields.filter(Boolean).length;

      // "All or Nothing" rule
      const allEmpty = filledCount === 0 && !hasPassword;
      const allFilled = filledCount === 4 && (isEditMode || hasPassword);

      if (!allEmpty && !allFilled) {
        errors.push('Admin fields are all-or-nothing: provide all admin fields or leave them all empty');
      }

      // If admin is being added/updated, validate formats
      if (!allEmpty) {
        if (hasEmail && !emailRegex.test(admin.Email!)) {
          errors.push('Tenant admin email format is invalid');
        }
        if (hasPassword && admin.Password!.length < 8) {
          errors.push('Tenant admin password must be at least 8 characters');
        }
      }
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
        NumberOfEmployees: null,
        Location: '',
        Industry: '',
        ContactPersonName: '',
        ContactPersonEmail: '',
        ContactPersonRole: null,
        ContactPersonPhone: null,
        LogoUrl: null,
      },
      TenantAdmin: {
        FirstName: null,
        LastName: null,
        Email: null,
        PhoneNumber: null,
        Password: null,
      },
    };
  }
}

// Create and export a singleton instance
export const tenantService = new TenantService();

// Export the class for testing or custom instances
export { TenantService };
