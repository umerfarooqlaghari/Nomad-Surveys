/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, ApiResponse, handleApiResponse } from './api';

// Types for Evaluator API
export interface Evaluator {
  id: string;
  evaluatorFirstName: string;
  evaluatorLastName: string;
  evaluatorEmail: string;
  evaluatorPhoneNumber: string;
  evaluatorDepartment: string;
  evaluatorPosition: string;
  isActive: boolean;
  createdAt: string;
  updatedAt?: string;
  tenantId: string;
}

export interface CreateEvaluatorRequest {
  evaluatorFirstName: string;
  evaluatorLastName: string;
  evaluatorEmail: string;
  evaluatorPhoneNumber?: string;
  evaluatorDepartment?: string;
  evaluatorPosition?: string;
}

export interface UpdateEvaluatorRequest {
  evaluatorFirstName: string;
  evaluatorLastName: string;
  evaluatorEmail: string;
  evaluatorPhoneNumber?: string;
  evaluatorDepartment?: string;
  evaluatorPosition?: string;
}

export interface BulkCreateEvaluatorsRequest {
  evaluators: CreateEvaluatorRequest[];
}

export interface BulkCreateResponse {
  successCount: number;
  errorCount: number;
  errors: string[];
  createdEvaluators: Evaluator[];
}

class EvaluatorService {
  /**
   * Get all evaluators for a tenant
   */
  async getEvaluators(tenantSlug: string, token: string): Promise<{ data: Evaluator[] | null; error: string | null }> {
    const response = await apiClient.get<Evaluator[]>(`/${tenantSlug}/evaluators`, token);
    return handleApiResponse(response);
  }

  /**
   * Get evaluator by ID
   */
  async getEvaluatorById(tenantSlug: string, evaluatorId: string, token: string): Promise<{ data: Evaluator | null; error: string | null }> {
    const response = await apiClient.get<Evaluator>(`/${tenantSlug}/evaluators/${evaluatorId}`, token);
    return handleApiResponse(response);
  }

  /**
   * Create a new evaluator
   */
  async createEvaluator(tenantSlug: string, evaluatorData: CreateEvaluatorRequest, token: string): Promise<{ data: Evaluator | null; error: string | null }> {
    const response = await apiClient.post<Evaluator>(`/${tenantSlug}/evaluators`, evaluatorData, token);
    return handleApiResponse(response);
  }

  /**
   * Update an existing evaluator
   */
  async updateEvaluator(tenantSlug: string, evaluatorId: string, evaluatorData: UpdateEvaluatorRequest, token: string): Promise<{ data: Evaluator | null; error: string | null }> {
    const response = await apiClient.put<Evaluator>(`/${tenantSlug}/evaluators/${evaluatorId}`, evaluatorData, token);
    return handleApiResponse(response);
  }

  /**
   * Delete an evaluator
   */
  async deleteEvaluator(tenantSlug: string, evaluatorId: string, token: string): Promise<{ data: any | null; error: string | null }> {
    const response = await apiClient.delete(`/${tenantSlug}/evaluators/${evaluatorId}`, token);
    return handleApiResponse(response);
  }

  /**
   * Bulk create evaluators
   */
  async bulkCreateEvaluators(tenantSlug: string, request: BulkCreateEvaluatorsRequest, token: string): Promise<{ data: BulkCreateResponse | null; error: string | null }> {
    const response = await apiClient.post<BulkCreateResponse>(`/${tenantSlug}/evaluators/bulk`, request, token);
    return handleApiResponse(response);
  }

  /**
   * Parse CSV data into evaluators array
   */
  parseCSV(csvData: string): CreateEvaluatorRequest[] {
    const lines = csvData.trim().split('\n');
    const headers = lines[0].split(',').map(h => h.trim());
    
    const evaluators: CreateEvaluatorRequest[] = [];
    
    for (let i = 1; i < lines.length; i++) {
      const values = lines[i].split(',').map(v => v.trim());
      
      if (values.length >= headers.length) {
        const evaluator: CreateEvaluatorRequest = {
          evaluatorFirstName: values[headers.indexOf('evaluatorFirstName')] || '',
          evaluatorLastName: values[headers.indexOf('evaluatorLastName')] || '',
          evaluatorEmail: values[headers.indexOf('evaluatorEmail')] || '',
          evaluatorPhoneNumber: values[headers.indexOf('evaluatorPhoneNumber')] || '',
          evaluatorDepartment: values[headers.indexOf('evaluatorDepartment')] || '',
          evaluatorPosition: values[headers.indexOf('evaluatorPosition')] || '',
        };
        
        if (evaluator.evaluatorFirstName && evaluator.evaluatorLastName && evaluator.evaluatorEmail) {
          evaluators.push(evaluator);
        }
      }
    }
    
    return evaluators;
  }

  /**
   * Generate CSV template
   */
  generateCSVTemplate(): string {
    return "evaluatorFirstName,evaluatorLastName,evaluatorEmail,evaluatorPhoneNumber,evaluatorDepartment,evaluatorPosition\nJane,Smith,jane.smith@example.com,+1234567890,Management,Team Lead";
  }

  /**
   * Validate evaluator data
   */
  validateEvaluator(evaluator: CreateEvaluatorRequest): string[] {
    const errors: string[] = [];
    
    if (!evaluator.evaluatorFirstName?.trim()) {
      errors.push('First name is required');
    }
    
    if (!evaluator.evaluatorLastName?.trim()) {
      errors.push('Last name is required');
    }
    
    if (!evaluator.evaluatorEmail?.trim()) {
      errors.push('Email is required');
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(evaluator.evaluatorEmail)) {
      errors.push('Invalid email format');
    }
    
    return errors;
  }
}

// Create and export a singleton instance
export const evaluatorService = new EvaluatorService();

// Export the class for testing or custom instances
export { EvaluatorService };
