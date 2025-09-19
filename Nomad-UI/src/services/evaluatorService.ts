/* eslint-disable @typescript-eslint/no-explicit-any */
import { apiClient, handleApiResponse } from './api';

// Types for Evaluator API
export interface Evaluator {
  Id: string;
  FirstName: string;
  LastName: string;
  FullName: string;
  EvaluatorEmail: string;
  EmployeeId: string;
  CompanyName?: string;
  Gender?: string;
  BusinessUnit?: string;
  Grade?: string;
  Designation?: string;
  Tenure?: number;
  Location?: string;
  Metadata1?: string;
  Metadata2?: string;
  IsActive: boolean;
  CreatedAt: string;
  UpdatedAt?: string;
  TenantId: string;
  AssignedSubjectIds?: string[];
}

// List response has fewer fields than full evaluator response
export interface EvaluatorListResponse {
  Id: string;
  FirstName: string;
  LastName: string;
  FullName: string;
  EvaluatorEmail: string;
  EmployeeId: string;
  CompanyName?: string;
  Designation?: string;
  Location?: string;
  IsActive: boolean;
  CreatedAt: string;
  LastLoginAt?: string;
  TenantId: string;
  SubjectCount: number;
}

export interface CreateEvaluatorRequest {
  FirstName: string;
  LastName: string;
  EvaluatorEmail: string;
  EmployeeId: string;
  CompanyName?: string;
  Gender?: string;
  BusinessUnit?: string;
  Grade?: string;
  Designation?: string;
  Tenure?: number;
  Location?: string;
  Metadata1?: string;
  Metadata2?: string;
  RelatedEmployeeIds?: string[];
}

export interface UpdateEvaluatorRequest {
  FirstName: string;
  LastName: string;
  EvaluatorEmail: string;
  EmployeeId: string;
  CompanyName?: string;
  Gender?: string;
  BusinessUnit?: string;
  Grade?: string;
  Designation?: string;
  Tenure?: number;
  Location?: string;
  Metadata1?: string;
  Metadata2?: string;
  RelatedEmployeeIds?: string[];
}

export interface BulkCreateEvaluatorsRequest {
  Evaluators: CreateEvaluatorRequest[];
}

export interface ValidationResult {
  EmployeeId: string;
  IsValid: boolean;
  Message?: string;
  Data?: {
    Id: string;
    EmployeeId: string;
    FullName: string;
    Email: string;
    IsActive: boolean;
  };
}

export interface ValidationResponse {
  Results: ValidationResult[];
  TotalRequested: number;
  ValidCount: number;
  InvalidCount: number;
}

export interface BulkCreateResponse {
  TotalRequested: number;
  SuccessfullyCreated: number;
  UpdatedCount: number;
  Failed: number;
  Errors: string[];
  CreatedIds: string[];
}

class EvaluatorService {
  /**
   * Get all evaluators for a tenant
   */
  async getEvaluators(tenantSlug: string, token: string): Promise<{ data: EvaluatorListResponse[] | null; error: string | null }> {
    const response = await apiClient.get<EvaluatorListResponse[]>(`/${tenantSlug}/evaluators`, token);
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
   * Create a new evaluator (uses bulk endpoint for single evaluator)
   */
  async createEvaluator(tenantSlug: string, evaluatorData: CreateEvaluatorRequest, token: string): Promise<{ data: Evaluator | null; error: string | null }> {
    // Use bulk endpoint for single evaluator creation
    const bulkRequest: BulkCreateEvaluatorsRequest = {
      Evaluators: [evaluatorData]
    };

    const response = await apiClient.post<BulkCreateResponse>(`/${tenantSlug}/evaluators/bulk`, bulkRequest, token);
    const result = handleApiResponse(response);

    if (result.error) {
      return { data: null, error: result.error };
    }

    if (result.data && result.data.SuccessfullyCreated > 0) {
      // For single evaluator creation, we need to fetch the created evaluator
      // Since bulk response only returns IDs, we'll return a success indicator
      return { data: null, error: null }; // Success but no evaluator data returned
    } else {
      const errorMessage = result.data?.Errors?.[0] || 'Failed to create evaluator';
      return { data: null, error: errorMessage };
    }
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
      // Handle CSV parsing with quoted JSON arrays
      const line = lines[i];
      const values: string[] = [];
      let current = '';
      let inQuotes = false;

      for (let j = 0; j < line.length; j++) {
        const char = line[j];
        if (char === '"') {
          inQuotes = !inQuotes;
          current += char; // Include quotes in the current value
        } else if (char === ',' && !inQuotes) {
          values.push(current.trim());
          current = '';
        } else {
          current += char;
        }
      }
      values.push(current.trim());

      if (values.length >= headers.length) {
        // Parse RelatedEmployeeIds JSON array
        let relatedEmployeeIds: string[] | undefined;
        const relatedEmployeeIdsIndex = headers.indexOf('relatedEmployeeIds');
        if (relatedEmployeeIdsIndex !== -1 && values[relatedEmployeeIdsIndex]) {
          try {
            const jsonStr = values[relatedEmployeeIdsIndex].replace(/^"|"$/g, ''); // Remove outer quotes
            relatedEmployeeIds = JSON.parse(jsonStr);
          } catch (error) {
            console.warn('Failed to parse RelatedEmployeeIds JSON:', values[relatedEmployeeIdsIndex]);
            relatedEmployeeIds = undefined;
          }
        }

        const evaluator: CreateEvaluatorRequest = {
          FirstName: values[headers.indexOf('firstName')] || '',
          LastName: values[headers.indexOf('lastName')] || '',
          EvaluatorEmail: values[headers.indexOf('evaluatorEmail')] || '',
          EmployeeId: values[headers.indexOf('employeeId')] || '',
          CompanyName: values[headers.indexOf('companyName')] || '',
          Gender: values[headers.indexOf('gender')] || '',
          BusinessUnit: values[headers.indexOf('businessUnit')] || '',
          Grade: values[headers.indexOf('grade')] || '',
          Designation: values[headers.indexOf('designation')] || '',
          Tenure: parseInt(values[headers.indexOf('tenure')]) || undefined,
          Location: values[headers.indexOf('location')] || '',
          Metadata1: values[headers.indexOf('metadata1')] || '',
          Metadata2: values[headers.indexOf('metadata2')] || '',
          RelatedEmployeeIds: relatedEmployeeIds,
        };

        if (evaluator.FirstName && evaluator.LastName && evaluator.EvaluatorEmail && evaluator.EmployeeId) {
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
    return 'firstName,lastName,evaluatorEmail,employeeId,companyName,gender,businessUnit,grade,designation,tenure,location,metadata1,metadata2,relatedEmployeeIds\nJane,Smith,jane.smith@example.com,EVL001,Acme Corp,Female,HR,Manager,HR Manager,3,Boston,Team Lead,People Operations,"[""SUB001"", ""SUB002""]"';
  }

  /**
   * Validate evaluator data
   */
  validateEvaluator(evaluator: CreateEvaluatorRequest): string[] {
    const errors: string[] = [];

    if (!evaluator.FirstName?.trim()) {
      errors.push('First name is required');
    }

    if (!evaluator.LastName?.trim()) {
      errors.push('Last name is required');
    }

    if (!evaluator.EvaluatorEmail?.trim()) {
      errors.push('Email is required');
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(evaluator.EvaluatorEmail)) {
      errors.push('Invalid email format');
    }

    if (!evaluator.EmployeeId?.trim()) {
      errors.push('Employee ID is required');
    } else if (evaluator.EmployeeId.length > 50) {
      errors.push('Employee ID must be 50 characters or less');
    }

    return errors;
  }

  /**
   * Validate subject EmployeeIds for relationship creation
   */
  async validateSubjectIds(tenantSlug: string, employeeIds: string[], token: string): Promise<string[]> {
    try {
      const response = await apiClient.post(`/${tenantSlug}/evaluators/validate-subject-ids`, employeeIds, token);
      console.log('Raw evaluator API response:', response);

      // Handle different response formats based on status code
      if (response.status === 200) {
        // Single valid ID - response.data contains subject info
        console.log('Single evaluator ID validation - returning first ID');
        return [employeeIds[0]];
      } else if (response.status === 207) {
        // Multiple IDs - response.data contains ValidationResponse
        const apiResponse = response.data as any;
        console.log('Multi-status evaluator response received:', apiResponse);

        // Extract valid Employee IDs from the response
        const validEmployeeIds: string[] = [];

        if (apiResponse && apiResponse.Results && Array.isArray(apiResponse.Results)) {
          console.log(`Processing ${apiResponse.Results.length} evaluator results...`);

          for (let i = 0; i < apiResponse.Results.length; i++) {
            const result = apiResponse.Results[i];
            console.log(`Evaluator Result ${i}:`, {
              EmployeeId: result.EmployeeId,
              IsValid: result.IsValid,
              Message: result.Message
            });

            // Check if this result is valid
            if (result.IsValid === true) {
              console.log(`✓ ${result.EmployeeId} is valid - adding to evaluator array`);
              validEmployeeIds.push(result.EmployeeId);
            } else {
              console.log(`✗ ${result.EmployeeId} is invalid - skipping`);
            }
          }
        } else {
          console.error('Invalid evaluator response structure:', apiResponse);
        }

        console.log('Final valid evaluator Employee IDs array:', validEmployeeIds);
        return validEmployeeIds;
      } else {
        // 404 or other error - no valid IDs
        console.log(`Unexpected evaluator status code: ${response.status}`);
        return [];
      }
    } catch (error: any) {
      // Handle 404 Not Found for single invalid ID
      if (error.response?.status === 404) {
        console.log('404 evaluator error - no valid IDs');
        return [];
      }
      console.error('Error validating subject EmployeeIds:', error);
      return [];
    }
  }
}

// Create and export a singleton instance
export const evaluatorService = new EvaluatorService();

// Export the class for testing or custom instances
export { EvaluatorService };
