/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, ApiResponse, handleApiResponse } from './api';

// Types for Subject API
export interface Subject {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber: string;
  department: string;
  position: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  tenantId: string;
}

export interface CreateSubjectRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  department?: string;
  position?: string;
}

export interface UpdateSubjectRequest {
  firstName: string;
  lastName: string;
  email: string;
  phoneNumber?: string;
  department?: string;
  position?: string;
}

export interface BulkCreateSubjectsRequest {
  subjects: CreateSubjectRequest[];
}

export interface BulkCreateResponse {
  successCount: number;
  errorCount: number;
  errors: string[];
  createdSubjects: Subject[];
}

class SubjectService {
  /**
   * Get all subjects for a tenant
   */
  async getSubjects(tenantSlug: string, token: string): Promise<{ data: Subject[] | null; error: string | null }> {
    const response = await apiClient.get<Subject[]>(`/${tenantSlug}/subjects`, token);
    return handleApiResponse(response);
  }

  /**
   * Get subject by ID
   */
  async getSubjectById(tenantSlug: string, subjectId: string, token: string): Promise<{ data: Subject | null; error: string | null }> {
    const response = await apiClient.get<Subject>(`/${tenantSlug}/subjects/${subjectId}`, token);
    return handleApiResponse(response);
  }

  /**
   * Create a new subject
   */
  async createSubject(tenantSlug: string, subjectData: CreateSubjectRequest, token: string): Promise<{ data: Subject | null; error: string | null }> {
    const response = await apiClient.post<Subject>(`/${tenantSlug}/subjects`, subjectData, token);
    return handleApiResponse(response);
  }

  /**
   * Update an existing subject
   */
  async updateSubject(tenantSlug: string, subjectId: string, subjectData: UpdateSubjectRequest, token: string): Promise<{ data: Subject | null; error: string | null }> {
    const response = await apiClient.put<Subject>(`/${tenantSlug}/subjects/${subjectId}`, subjectData, token);
    return handleApiResponse(response);
  }

  /**
   * Delete a subject
   */
  async deleteSubject(tenantSlug: string, subjectId: string, token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.delete(`/${tenantSlug}/subjects/${subjectId}`, token);
    return handleApiResponse(response);
  }

  /**
   * Bulk create subjects
   */
  async bulkCreateSubjects(tenantSlug: string, request: BulkCreateSubjectsRequest, token: string): Promise<{ data: BulkCreateResponse | null; error: string | null }> {
    const response = await apiClient.post<BulkCreateResponse>(`/${tenantSlug}/subjects/bulk`, request, token);
    return handleApiResponse(response);
  }

  /**
   * Parse CSV data into subjects array
   */
  parseCSV(csvData: string): CreateSubjectRequest[] {
    const lines = csvData.trim().split('\n');
    const headers = lines[0].split(',').map(h => h.trim());
    
    const subjects: CreateSubjectRequest[] = [];
    
    for (let i = 1; i < lines.length; i++) {
      const values = lines[i].split(',').map(v => v.trim());
      
      if (values.length >= headers.length) {
        const subject: CreateSubjectRequest = {
          firstName: values[headers.indexOf('firstName')] || '',
          lastName: values[headers.indexOf('lastName')] || '',
          email: values[headers.indexOf('email')] || '',
          phoneNumber: values[headers.indexOf('phoneNumber')] || '',
          department: values[headers.indexOf('department')] || '',
          position: values[headers.indexOf('position')] || '',
        };
        
        if (subject.firstName && subject.lastName && subject.email) {
          subjects.push(subject);
        }
      }
    }
    
    return subjects;
  }

  /**
   * Generate CSV template
   */
  generateCSVTemplate(): string {
    return "firstName,lastName,email,phoneNumber,department,position\nJohn,Doe,john.doe@example.com,+1234567890,Engineering,Software Engineer";
  }

  /**
   * Validate subject data
   */
  validateSubject(subject: CreateSubjectRequest): string[] {
    const errors: string[] = [];
    
    if (!subject.firstName?.trim()) {
      errors.push('First name is required');
    }
    
    if (!subject.lastName?.trim()) {
      errors.push('Last name is required');
    }
    
    if (!subject.email?.trim()) {
      errors.push('Email is required');
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(subject.email)) {
      errors.push('Invalid email format');
    }
    
    return errors;
  }
}

// Create and export a singleton instance
export const subjectService = new SubjectService();

// Export the class for testing or custom instances
export { SubjectService };
